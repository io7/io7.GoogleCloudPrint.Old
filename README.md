# GoogleCloudPrint for C#

**This repository shamelessly copies code from https://github.com/chrisntr/GoogleCloudPrintMonodroid**

The problem with the Monodroid implementation is that it uses the old OpenID authentication mechanism, which Google has since deprecated.

This library uses Google's OAuth2 library from https://www.nuget.org/packages/Google.Apis.Oauth2.v2/

## Usage

1. Create a service API account at https://console.developers.google.com:
  Go to APIs & auth / Credentials, click Add credentials and select Service account.
2. Generate a P12 key based on the service account you just made and export it. Place this file in your project folder and ensure the file properties are set to Content and Copy if newer.
3. Go to the Credentials overview in the Google Developer Console and find the email that was generated for your service account. You need to share your printer with this email in order for the service account to be able to print to your printer.
4. The problem with this is that a service account can't accept a printer share request sent from Google. For this reason, you need to create a Google Private Group (can be done within Google Apps for Business) which includes a normal e-mail and the service account's email. Make your own email an owner, and the service account email a member.
5. Share your printer with the group's email, then log in with your own account and accept the share request (on behalf of the entire group). Your service account now has access to the printer!
6. Now let's test if it works. Use the following syntax to connect to cloud print (The "source" parameter is just a name for your application, for example "io7-googlecloudprint"):

```csharp
var gcp = new GoogleCloudPrintService("< your service account e-mail >", "< P12 file location >", "< P12 secret >", "< source >");

var printerCollection = gcp.GetPrinters();

foreach (var printer in printerCollection.printers)
{
    Console.WriteLine(printer.name);
}
```