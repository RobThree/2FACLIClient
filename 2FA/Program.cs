using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Parsing;
using System.Data;
using System.Text;
using System.Text.Json;
using TwoFA.Encryption;
using TwoFA.Models;

namespace TwoFA;

public class Program
{
    private static readonly ConcurrentDictionary<TwoFAOptions, TwoFACalculator> _calculators = new();

    private static int Main(string[] args)
    {
        var dp = new AesDataProtector();
        var fileOption = new Option<FileInfo>("--file", "The TOTP file.") { IsRequired = true };
        fileOption.AddAlias("-i");

        var findOption = new Option<string>("--find", "Search string.") { IsRequired = true };
        findOption.AddAlias("-f");

        var rootCommand = new RootCommand("LastPass 2FA CLI authenticator");

        var encryptcommand = new Command("encrypt", "Encrypts a given TOTP password file.") { fileOption };
        encryptcommand.SetHandler(Encrypt, fileOption);

        var findcommand = new Command("find", "Finds and shows matching TOTP codes") { fileOption, findOption };
        findcommand.SetHandler(Find, fileOption, findOption);

        rootCommand.AddCommand(encryptcommand);
        rootCommand.AddCommand(findcommand);

        return new CommandLineBuilder(rootCommand)
            .UseDefaults()
            .UseExceptionHandler((e, context) => WriteConsoleError(e.Message), -1)
            .Build()
            .Invoke(args);
    }

    private static void Encrypt(FileInfo secretsFile)
    {
        var dp = new AesDataProtector();
        var password = ReadPassword("Password");
        if (ReadPassword("Confirm password") == password)
        {
            dp.EncryptFile(secretsFile.FullName, password);
        }
        else
        {
            WriteConsoleError("Password and confirmed password do not match.\nFile was not encrypted");
        }
    }

    private static void Find(FileInfo secretsFile, string find)
    {
        var dp = new AesDataProtector();
        var password = ReadPassword("Password");
        var json = dp.DecryptFile(secretsFile.FullName, password);
        var accounts = JsonSerializer.Deserialize<TwoFASecretsFile>(json)!.Accounts.Where(account => IsMatch(account, find));

        foreach (var account in accounts)
        {
            var options = new TwoFAOptions { Digits = account.Digits, Period = account.TimeStep, Algorithm = Enum.Parse<Algorithm>(account.Algorithm) };
            var calc = _calculators.GetOrAdd(options, o => new TwoFACalculator(Options.Create(o)));
            Console.WriteLine(
                $"{account.UserName}@{account.IssuerName} ({account.OriginalUserName}@{account.OriginalIssuerName}) {calc.GetCode(account.Secret)}"
            );
        }
    }

    private static void WriteConsoleError(string error)
    {
        var color = Console.ForegroundColor;
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine(error);
        Console.ForegroundColor = color;
    }

    private static bool IsMatch(TwoFAAccount account, string find, StringComparison stringComparison = StringComparison.OrdinalIgnoreCase)
        => account.UserName.Contains(find, stringComparison)
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