using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Parsing;
using System.Text;
using System.Text.Json;
using TwoFA.Encryption;
using TwoFA.Models;

namespace TwoFA;

internal class Program
{
    private static readonly ConcurrentDictionary<TwoFAOptions, TwoFACalculator> _calculators = new();
    private static readonly AesDataProtectorOptions _vaultoptions = new();
    private static readonly MainOptions _mainoptions = new();

    private static int Main(string[] args)
    {
        var configprovider = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", true)
            .AddUserSecrets<Program>()
            .Build();

        configprovider.Bind("Main", _mainoptions);
        configprovider.Bind("Vault", _vaultoptions);

        var fileOption = new Option<FileInfo>("--file", "The TOTP file");
        fileOption.AddAlias("-i");
        if (!string.IsNullOrEmpty(_mainoptions.VaultFile))
        {
            fileOption.SetDefaultValue(new FileInfo(_mainoptions.VaultFile));
        }

        var findOption = new Option<string?>("--find", "Search string");
        findOption.AddAlias("-f");

        var usernameOption = new Option<string>("--username", "Username (emailaddress)") { IsRequired = true };
        usernameOption.AddAlias("-u");

        var otpOption = new Option<string>("--otp", "Current OTP code");
        otpOption.AddAlias("-o");

        var rootCommand = new RootCommand("LastPass 2FA CLI authenticator");

        var refreshcommand = new Command("refresh", "Get or refresh TOTP password vault") { fileOption, usernameOption, otpOption };
        refreshcommand.AddAlias("update");
        refreshcommand.AddAlias("download");
        refreshcommand.SetHandler(Refresh, fileOption, usernameOption, otpOption);

        var listcommand = new Command("list", "Lists accounts and matching TOTP codes") { fileOption, findOption };
        listcommand.SetHandler(List, fileOption, findOption);

        rootCommand.AddCommand(refreshcommand);
        rootCommand.AddCommand(listcommand);

        return new CommandLineBuilder(rootCommand)
            .UseDefaults()
            .UseExceptionHandler((e, context) => WriteConsoleError(e.Message), -1)
            .Build()
            .Invoke(args);
    }

    private static async Task Refresh(FileInfo vaultFile, string username, string? otp)
    {
        var lppassword = ReadPassword("LastPass master password");
        var dp = new AesDataProtector(Options.Create(_vaultoptions));
        var lpclient = new LastPassMFABackupDownloader();
        var mfadata = await lpclient.DownloadAsync(username, lppassword, otp).ConfigureAwait(false);
        Console.WriteLine("MFA Backup successfully retrieved");
        var password = ReadPassword("Local vault password");
        if (ReadPassword("Confirm local vault password") == password)
        {
            dp.SaveEncrypted(vaultFile.FullName, mfadata, password);
        }
        Console.WriteLine("Local vault updated / refreshed");
    }

    private static void List(FileInfo vaultFile, string? find)
    {
        var dp = new AesDataProtector(Options.Create(_vaultoptions));
        var password = ReadPassword("Password");
        var json = dp.LoadEncrypted(vaultFile.FullName, password);
        var accounts = JsonSerializer.Deserialize<TwoFASecretsFile>(json)!.Accounts.ToArray();

        var matchingaccounts = accounts.Where(account => IsMatch(account, find))
            .OrderBy(account => account.IssuerName)
            .ThenBy(account => account.UserName)
            .ThenBy(account => account.CreationDate)
            .ToArray();
        var maxlen = matchingaccounts.Max(a => (a.IssuerName ?? string.Empty).Length);

        Console.WriteLine($"{accounts.Length} accounts in store");
        foreach (var account in matchingaccounts)
        {
            var options = new TwoFAOptions { Digits = account.Digits, Period = account.TimeStep, Algorithm = Enum.Parse<Algorithm>(account.Algorithm) };
            var calc = _calculators.GetOrAdd(options, o => new TwoFACalculator(Options.Create(o)));
            Console.WriteLine(
                $"{account.IssuerName.PadRight(maxlen)} : {calc.GetCode(account.Secret),-10} ({account.UserName}, {account.OriginalIssuerName})"
            );
        }
        Console.WriteLine($"{matchingaccounts.Length} accounts matched");
    }

    private static void WriteConsoleError(string error)
    {
        var color = Console.ForegroundColor;
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine(error);
        Console.ForegroundColor = color;
    }

    private static bool IsMatch(TwoFAAccount account, string? find, StringComparison stringComparison = StringComparison.OrdinalIgnoreCase)
        => find is null
            || account.UserName.Contains(find, stringComparison)
            || account.IssuerName.Contains(find, stringComparison)
            || account.OriginalUserName.Contains(find, stringComparison)
            || account.OriginalIssuerName.Contains(find, stringComparison);

    private static string ReadPassword(string prompt)
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
        } while (!done);

        return password.ToString();
    }
}