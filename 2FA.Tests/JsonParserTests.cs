using System.Text.Json;
using TwoFA.Models;

namespace TwoFA.Tests;

[TestClass]
public class JsonParserTests
{
    [TestMethod]
    public void CanParseV1Data()
    {
        var secretsfile = JsonSerializer.Deserialize<TwoFASecretsFile>(File.ReadAllText("data/v1.json"));

        Assert.IsNotNull(secretsfile);
        Assert.AreEqual(1, secretsfile.Version);
        Assert.AreEqual("device-name", secretsfile.DeviceName);
        Assert.AreEqual("device-secret", secretsfile.DeviceSecret);
        Assert.AreEqual(Guid.Parse("4f836418-f455-4044-9d7a-755024532710"), secretsfile.DeviceId);
        Assert.AreEqual(Guid.Parse("261ce3ca-32a1-4b30-b35f-03e7eaa95a56"), secretsfile.LocalDeviceId);

        Assert.AreEqual(2, secretsfile.Accounts.Count());
        Assert.AreEqual(0, secretsfile.Folders.Count());

        var accounts = secretsfile.Accounts.ToArray();

        Assert.AreEqual(Guid.Parse("62f89159-925e-4cde-b90f-e4a8126f7eca"), accounts[0].AccountID);
        Assert.AreEqual(Guid.Parse("34d8ab9c-4b9b-4e88-81c9-c40e56c2e0e6"), accounts[1].AccountID);
        Assert.IsNull(accounts[0].LmiUserId);
        Assert.IsNull(accounts[1].LmiUserId);
        Assert.AreEqual("issuer 1", accounts[0].IssuerName);
        Assert.AreEqual("issuer 2", accounts[1].IssuerName);
        Assert.AreEqual("originalissuer 1", accounts[0].OriginalIssuerName);
        Assert.AreEqual("originalissuer 2", accounts[1].OriginalIssuerName);
        Assert.IsFalse(accounts[0].PushNotification);
        Assert.IsFalse(accounts[1].PushNotification);
        Assert.AreEqual("MZXW6YTBOJRGC6Q", accounts[0].Secret);
        Assert.AreEqual("NBSWY3DPORSXG5DFOJZQ", accounts[1].Secret);
        Assert.AreEqual(TimeSpan.FromSeconds(30), accounts[0].TimeStep);
        Assert.AreEqual(TimeSpan.FromSeconds(60), accounts[1].TimeStep);
        Assert.AreEqual(6, accounts[0].Digits);
        Assert.AreEqual(8, accounts[1].Digits);
        Assert.AreEqual(new DateTimeOffset(638261318110000000L, TimeSpan.Zero), accounts[0].CreationDate); // Second-resolution timestamp
        Assert.AreEqual(new DateTimeOffset(638261318100000000L, TimeSpan.Zero), accounts[1].CreationDate); // Second-resolution timestamp
        Assert.IsFalse(accounts[0].IsFavorite);
        Assert.IsFalse(accounts[1].IsFavorite);
        Assert.AreEqual(Algorithm.SHA1, accounts[0].Algorithm);
        Assert.AreEqual(Algorithm.SHA256, accounts[1].Algorithm);

        Assert.IsNull(accounts[0].FolderData);
        Assert.IsNull(accounts[1].FolderData);
    }

    [TestMethod]
    public void CanParseV3Data()
    {
        var secretsfile = JsonSerializer.Deserialize<TwoFASecretsFile>(File.ReadAllText("data/v3.json"));

        Assert.IsNotNull(secretsfile);
        Assert.AreEqual(3, secretsfile.Version);
        Assert.AreEqual("device-name", secretsfile.DeviceName);
        Assert.AreEqual("device-secret", secretsfile.DeviceSecret);
        Assert.AreEqual(Guid.Parse("4f836418-f455-4044-9d7a-755024532710"), secretsfile.DeviceId);
        Assert.AreEqual(Guid.Parse("261ce3ca-32a1-4b30-b35f-03e7eaa95a56"), secretsfile.LocalDeviceId);

        Assert.AreEqual(2, secretsfile.Accounts.Count());
        Assert.AreEqual(2, secretsfile.Folders.Count());

        var accounts = secretsfile.Accounts.ToArray();
        var folders = secretsfile.Folders.ToArray();

        Assert.AreEqual(Guid.Parse("62f89159-925e-4cde-b90f-e4a8126f7eca"), accounts[0].AccountID);
        Assert.AreEqual(Guid.Parse("34d8ab9c-4b9b-4e88-81c9-c40e56c2e0e6"), accounts[1].AccountID);
        Assert.AreEqual("lmi userid 1", accounts[0].LmiUserId);
        Assert.AreEqual("lmi userid 2", accounts[1].LmiUserId);
        Assert.AreEqual("issuer 1", accounts[0].IssuerName);
        Assert.AreEqual("issuer 2", accounts[1].IssuerName);
        Assert.AreEqual("originalissuer 1", accounts[0].OriginalIssuerName);
        Assert.AreEqual("originalissuer 2", accounts[1].OriginalIssuerName);
        Assert.IsTrue(accounts[0].PushNotification);
        Assert.IsFalse(accounts[1].PushNotification);
        Assert.AreEqual("MZXW6YTBOJRGC6Q", accounts[0].Secret);
        Assert.AreEqual("NBSWY3DPORSXG5DFOJZQ", accounts[1].Secret);
        Assert.AreEqual(TimeSpan.FromSeconds(30), accounts[0].TimeStep);
        Assert.AreEqual(TimeSpan.FromSeconds(60), accounts[1].TimeStep);
        Assert.AreEqual(6, accounts[0].Digits);
        Assert.AreEqual(8, accounts[1].Digits);
        Assert.AreEqual(new DateTimeOffset(638261318103210000L, TimeSpan.Zero), accounts[0].CreationDate); // Ensure millisecond-resolution timestamps are parsed correctly
        Assert.AreEqual(new DateTimeOffset(638261318100000000L, TimeSpan.Zero), accounts[1].CreationDate); // Second-resolution timestamp
        Assert.IsTrue(accounts[0].IsFavorite);
        Assert.IsFalse(accounts[1].IsFavorite);
        Assert.AreEqual(Algorithm.SHA1, accounts[0].Algorithm);
        Assert.AreEqual(Algorithm.SHA256, accounts[1].Algorithm);

        Assert.IsNotNull(accounts[0].FolderData);
        Assert.IsNotNull(accounts[1].FolderData);

        Assert.AreEqual(1, accounts[0].FolderData?.FolderId);
        Assert.AreEqual(0, accounts[1].FolderData?.FolderId);
        Assert.AreEqual(10, accounts[0].FolderData?.Position);
        Assert.AreEqual(20, accounts[1].FolderData?.Position);

        Assert.AreEqual(1, folders[0].Id);
        Assert.AreEqual(0, folders[1].Id);
        Assert.AreEqual("folder 1", folders[0].Name);
        Assert.AreEqual("folder 2", folders[1].Name);
        Assert.IsFalse(folders[0].IsOpened);
        Assert.IsTrue(folders[1].IsOpened);
    }
}