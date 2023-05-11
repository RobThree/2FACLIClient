﻿using Microsoft.Extensions.Configuration;
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
    private static readonly ConcurrentDictionary<TwoFAOptions, TwoFACalculator> _calculators = new();
    private static readonly AesDataProtectorOptions _vaultoptions = new();
    private static readonly MainOptions _mainoptions = new();

    private static int Main(string[] args)
    {
        // This wil; only be invoked before the CommandLineBuilder is invoked.
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
        refreshcommand.SetHandler(Refresh, fileOption, usernameOption, otpOption);

        var listcommand = new Command("list", Translations.CMD_LIST) { fileOption, findOption };
        listcommand.SetHandler(List, fileOption, findOption);

        rootCommand.AddCommand(refreshcommand);
        rootCommand.AddCommand(listcommand);

        // Kick off application
        return new CommandLineBuilder(rootCommand)
            .UseDefaults()
            .UseExceptionHandler((e, context) => WriteConsoleError(e.Message), -1)
            .Build()
            .Invoke(args);
    }

    private static async Task Refresh(FileInfo vaultFile, string username, string? otp)
    {
        var lppassword = ReadPassword(Translations.PROMPT_LASTPASS_PWD);
        var dp = new AesDataProtector(Options.Create(_vaultoptions));
        var lpclient = new LastPassMFABackupDownloader();
        var mfadata = await lpclient.DownloadAsync(username, lppassword, otp).ConfigureAwait(false);
        Console.WriteLine(Translations.STATUS_MFA_BACKUP_DOWNLOADED);

        var password = ReadPassword(Translations.PROMPT_LOCALVAULT_PWD);

        if (password == lppassword)
        {
            WriteConsoleWarning(Translations.WARN_LPMASTERPASSWD_SAME);
        }

        if (password == lppassword || ReadPassword(Translations.PROMPT_CONFIRM_LOCALVAULT_PWD) == password)
        {
            dp.SaveEncrypted(vaultFile.FullName, mfadata, password);
        }
        Console.WriteLine(Translations.STATUS_LOCALVAULT_UPDATED);
    }

    private static void List(FileInfo vaultFile, string? find)
    {
        var dp = new AesDataProtector(Options.Create(_vaultoptions));
        var password = ReadPassword(Translations.PROMPT_LOCALVAULT_PWD);
        var json = dp.LoadEncrypted(vaultFile.FullName, password);
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
            var options = new TwoFAOptions { Digits = account.Digits, Period = account.TimeStep, Algorithm = Enum.Parse<Algorithm>(account.Algorithm) };
            var calc = _calculators.GetOrAdd(options, o => new TwoFACalculator(Options.Create(o)));
            Console.WriteLine(
                $"{account.IssuerName.PadRight(maxlen)} : {calc.GetCode(account.Secret),-10} ({account.UserName}, {account.OriginalIssuerName})"
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