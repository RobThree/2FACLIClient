using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Parsing;
using System.Globalization;
using System.Text;
using System.Text.Json;
using TwoFA.Encryption;
using TwoFA.Models;
using TwoFA.ResourceFiles;

namespace TwoFA;

internal class Program
{
    private static readonly ConcurrentDictionary<TwoFACalculatorOptions, TwoFACalculator> _calculators = new();
    private static readonly AesDataProtectorOptions _vaultoptions = new();
    private static readonly MainOptions _mainoptions = new();

    private static async Task<int> Main(string[] args)
    {
        // This will only be invoked before the CommandLineBuilder is invoked.
        AppDomain.CurrentDomain.UnhandledException += (s, e) =>
        {
            WriteConsoleError(string.Format(Translations.EX_UNHANDLED, (e.ExceptionObject as Exception)?.Message));
            Environment.Exit(-2);
        };

        // Read and apply config
        var configprovider = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", true)
            .AddUserSecrets<Program>()
            .Build();

        configprovider.Bind("Main", _mainoptions);
        configprovider.Bind("Vault", _vaultoptions);

        // Set app culture
        var culture = CultureInfo.GetCultureInfo(_mainoptions.Locale);
        Thread.CurrentThread.CurrentCulture = culture;
        Thread.CurrentThread.CurrentUICulture = culture;

        // Define commandline options
        var fileOption = new Option<FileInfo>("--file", "The TOTP file");
        fileOption.AddAlias("-i");
        if (!string.IsNullOrEmpty(_mainoptions.VaultFile))
        {
            fileOption.SetDefaultValue(new FileInfo(_mainoptions.VaultFile));
        }

        var findOption = new Option<string?>("--find", Translations.OPT_FIND);
        findOption.AddAlias("-f");

        var usernameOption = new Option<string>("--username", Translations.OPT_USERNAME) { IsRequired = true };
        usernameOption.AddAlias("-u");

        var otpOption = new Option<string>("--otp", Translations.OPT_OTP);
        otpOption.AddAlias("-o");

        var rootCommand = new RootCommand(Translations.CMD_ROOT);

        var refreshcommand = new Command("refresh", Translations.CMD_REFRESH) { fileOption, usernameOption, otpOption };
        refreshcommand.AddAlias("update");
        refreshcommand.AddAlias("download");
        refreshcommand.SetHandler(async (ctx) => await Refresh(
            ctx.ParseResult.GetValueForOption(fileOption)!,
            ctx.ParseResult.GetValueForOption(usernameOption)!,
            ctx.ParseResult.GetValueForOption(otpOption)!,
            ctx.GetCancellationToken()
        ).ConfigureAwait(false));

        var listcommand = new Command("list", Translations.CMD_LIST) { fileOption, findOption };
        listcommand.SetHandler(async (ctx) => await List(
            ctx.ParseResult.GetValueForOption(fileOption)!,
            ctx.ParseResult.GetValueForOption(findOption),
            ctx.GetCancellationToken()
        ).ConfigureAwait(false));

        rootCommand.AddCommand(refreshcommand);
        rootCommand.AddCommand(listcommand);

        // Kick off application
        return await new CommandLineBuilder(rootCommand)
            .UseDefaults()
            .UseExceptionHandler((e, context) => WriteConsoleError(e.Message), -1)
            .CancelOnProcessTermination()
            .Build()
            .InvokeAsync(args)
            .ConfigureAwait(false);
    }

    private static async Task Refresh(FileInfo vaultFile, string username, string? otp, CancellationToken cancellationToken = default)
    {
        var lppassword = ReadPassword(Translations.PROMPT_LASTPASS_PWD, cancellationToken);
        var dp = new AesDataProtector(Options.Create(_vaultoptions));
        var lpclient = new LastPassMFABackupDownloader();
        var mfadata = await lpclient.DownloadAsync(username, lppassword, otp, cancellationToken).ConfigureAwait(false);
        Console.WriteLine(Translations.STATUS_MFA_BACKUP_DOWNLOADED);

        var password = ReadPassword(Translations.PROMPT_LOCALVAULT_PWD, cancellationToken);

        if (password == lppassword)
        {
            WriteConsoleWarning(Translations.WARN_LPMASTERPASSWD_SAME);
        }

        if (password == lppassword || ReadPassword(Translations.PROMPT_CONFIRM_LOCALVAULT_PWD, cancellationToken) == password)
        {
            await dp.SaveEncryptedAsync(vaultFile.FullName, mfadata, password, cancellationToken).ConfigureAwait(false);
        }
        Console.WriteLine(Translations.STATUS_LOCALVAULT_UPDATED);
    }

    private static async Task List(FileInfo vaultFile, string? find, CancellationToken cancellationToken = default)
    {
        var dp = new AesDataProtector(Options.Create(_vaultoptions));
        var password = ReadPassword(Translations.PROMPT_LOCALVAULT_PWD, cancellationToken);
        var json = await dp.LoadEncryptedAsync(vaultFile.FullName, password, cancellationToken).ConfigureAwait(false);
        var accounts = JsonSerializer.Deserialize<TwoFASecretsFile>(json)!.Accounts.ToArray();

        var matchingaccounts = accounts.Where(account => IsMatch(account, find))
            .OrderBy(account => account.IssuerName)
            .ThenBy(account => account.UserName)
            .ThenBy(account => account.CreationDate)
            .ToArray();
        var maxlen = matchingaccounts.Max(a => (a.IssuerName ?? string.Empty).Length);

        Console.WriteLine(string.Format(Translations.STATUS_ACCOUNTS_IN_VAULT, accounts.Length));
        foreach (var account in matchingaccounts)
        {
            var options = new TwoFACalculatorOptions { Digits = account.Digits, Period = account.TimeStep, Algorithm = account.Algorithm };
            var calc = _calculators.GetOrAdd(options, o => new TwoFACalculator(Options.Create(o)));
            Console.WriteLine(
                $"{account.IssuerName?.PadRight(maxlen)} : {calc.GetCode(account.Secret),-10} ({string.Join(", ", new[] { account.UserName, account.OriginalIssuerName }.Where(s => !string.IsNullOrEmpty(s)).Distinct())})"
            );
        }
        Console.WriteLine(string.Format(Translations.STATUS_ACCOUNTS_MATCHED, matchingaccounts.Length));
    }

    private static void WriteConsoleError(string error) => WriteConsoleColor(error, ConsoleColor.Red);
    private static void WriteConsoleWarning(string warning) => WriteConsoleColor(warning, ConsoleColor.Yellow);

    private static void WriteConsoleColor(string text, ConsoleColor color)
    {
        var oldcolor = Console.ForegroundColor;
        Console.ForegroundColor = color;
        Console.WriteLine(text);
        Console.ForegroundColor = oldcolor;
    }

    private static bool IsMatch(TwoFAAccount account, string? find, StringComparison stringComparison = StringComparison.OrdinalIgnoreCase)
        => find is null
            || (account.UserName?.Contains(find, stringComparison) ?? false)
            || (account.IssuerName?.Contains(find, stringComparison) ?? false)
            || (account.OriginalUserName?.Contains(find, stringComparison) ?? false)
            || (account.OriginalIssuerName?.Contains(find, stringComparison) ?? false);

    private static string ReadPassword(string prompt, CancellationToken cancellationToken = default)
    {
        var password = new StringBuilder();
        var done = false;

        Console.Write($"{prompt}: ");
        do
        {
            var key = Console.ReadKey(true).KeyChar;
            switch (key)
            {
                case '\b' when password.Length > 0:
                    password.Remove(password.Length - 1, 1);
                    break;
                case '\r':
                    Console.WriteLine();
                    done = true;
                    break;
                default:
                    password.Append(key);
                    break;
            }
        } while (!done && !cancellationToken.IsCancellationRequested);

        return password.ToString();
    }
}