# GoogleCloudPrint for C&#35;

**This repository shamelessly copies code from https://github.com/chrisntr/GoogleCloudPrintMonodroid**

The problem with the Monodroid implementation is that it uses the old authentication mechanism, which Google has since deprecated.

This library uses Google's OAuth2 library from https://www.nuget.org/packages/Google.Apis.Oauth2.v2/. This enables you to print to a cloud enabled printer using a Service Account.

## Usage

1. Create a service API account at https://console.developers.google.com:
  Go to "Credentials", click "Create credentials" and select "Service account key".
2. Generate a P12 key based on the service account you just made and export it. Place this file in your project folder and ensure the file properties are set to "Content" and "Copy if newer".
3. Go to https://console.developers.google.com/iam-admin/serviceaccounts/ and save the "Service account ID" (email address) that was generated for your service account. You need to share your printer with this email address in order for the service account to be able to print to your printer.
4. Go to https://www.google.com/cloudprint/#printers and click "Details" for your printer.  Save the guid you see under "Advanced Details" > "Printer ID"
5. Click "Share" and paste the email address you copied in Step 3.
6. There are two ways to get your service account to accept the share invitation:
    
    ##### Using this library:

    1.  Run the following code in a new console program:
    
        ```csharp
        var gcp = new GoogleCloudPrintService("< your service account e-mail >", "< P12 file location >", "< P12 secret >", "gcp console test");
        
        Console.WriteLine("Accepting invite...");
        
        var printerid = "<Printer ID from Step 4>";
        var response = gcp.ProcessInvite(printerid);

        Console.WriteLine($"Result was: {response.success} {response.message}");
        
        Console.ReadLine();
        ```    

    ##### Creating a Google Private Group:

    1. Create a Google Private Group (can be done within Google Apps for Business) which includes a normal e-mail and the service account's email. Make your own email an owner, and the service account email a member.
    2. Share your printer with the group's email, then log in with your own account and accept the share request (on behalf of the entire group). Your service account now has access to the printer!
    
7. Now let's test if it works. Use the following syntax to connect to cloud print (The "source" parameter is just a name for your application, for example "io7-googlecloudprint"):

    ```csharp
    var gcp = new GoogleCloudPrintService("< your service account e-mail >", "< P12 file location >", "< P12 secret >", "< source >");
    
    var printerCollection = gcp.GetPrinters();
    
    foreach (var printer in printerCollection.printers)
    {
        Console.WriteLine(printer.name);
    }
    ```
