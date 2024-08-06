# Slush Store

Slush Store is an ASP.NET 8 (Core) MVC application that simulates an online storefront, featuring user account management, shopping cart functionality, order processing, and a secure integration with an external banking web API [companion application](https://github.com/JSlush95/BankingApp). This interaction is for payment and refund transactions. This application leverages ASP.NET Identity for user authentication and authorization, and Entity Framework Core for data access.

## Table of Contents

- [Features](#features)
- [Getting Started](#getting-started)
    - [Prerequisites](#prerequisites)
    - [Installation](#installation)
    - [Configuration](#configuration)
- [Usage](#usage)
    - [Bank API Integration/Security](#bank-api-integrationsecurity)
    - [User Account Management (Account Controller)](#user-account-management-account-controller)
    - [Store Account Management (Manage Controller)](#store-account-management-manage-controller)
    - [Product and Store Displays (Home Controller)](#product-and-store-displays-home-controller)

## Features

- User Registration and Authentication with ASP.NET Identity Core
- Data Management with Entity Framework Core
- Shopping Cart Management
- Order Placement and Tracking
- Integration with an External Banking Web API For Purchases
- Two-Factor Authentication (2FA)
- Email Confirmation for Accounts

## Getting Started

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [SQL Server](https://www.microsoft.com/en-us/sql-server/sql-server-downloads)
   - Note: this is for LocalDB testing, which this project is configured to. **Visual Studio** implements SQL Server natively. Alternative database implementation will require a different access provider in ``ApplicationDBContext.cs``. Entity Framework has some providers available. For example, Postgresql has a npgsql EF provider, which will use different database interaction syntax than what was used originally, requiring a possible migration schema change.

### Configuration - C# .NET 8 (.NET Core) - Program Variables

In .NET Core, you can configure the connection to a LocalDB SQL Server using a local JSON file (`appsettings.json`). Here’s how to set it up.<br>

1. Inspect the ``Program.cs`` file to see the switch case and how it references the provider cases, or add your own. The ``.json`` file configuration is used by default. The ``Program.cs`` will be the entry point to initializing services and ``.json`` references, so make sure everything is copied properly.

2. **Verify `appsettings.json` contents:**

    Ensure the connection string and provider for the LocalDB SQL Server is present in the `appsettings.json` or the upcoming section's ``secrets.json`` file:
    ```
    {
      "ConnectionStrings": {
        "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=BankingAppDb;Trusted_Connection=True;MultipleActiveResultSets=true"
      }
    ```
    
    **Requires a secondary external ``secrets.json`` file**<br>
    Make sure to generate the private key (the banking website uses this private key):
    - Note: Change the "SupabaseConnection" string's key and value to whatever your PostgresQL server may be. Otherwise, remove its references in this file and ``Program.cs``'s switch case. Tailor this to your needs.
    
    ```{
      "AppSettings": {
        "MailAccount": "...",     // The Email Sender such as name@gmail.com
        "MailPassword": "...",     // The Email Password
        "SmtpHost": "...",       // Smtp Email Account such as smtp.gmail.com
        "PublicKey": "..."  // Public key to encrypt data sent to the Bank API
      },
      "ConnectionStrings": {
        "SupabaseConnection": "Host=...;Database=...;Username=...;Password=...;"
      },
      "DatabaseProvider": "Postgresql"
        }
    ```
    
    Example of how I generated a cryptographic pair of private and public keys:
    ```
    RSACryptoServiceProvider rsa = new RSACryptoServiceProvider(2048);
    
    // RSA keys in XML format
    string publicPrivateKeyXML = rsa.ToXmlString(true);
    string publicOnlyKeyXML = rsa.ToXmlString(false);
    
    // Export to file, etc
    ```

2. **Verify the DbContext Content:**

    In your `Startup.cs` or `Program.cs` file, verify that the DbContext references the connection string from `appsettings.json`.<br>
    Default example for ``Startup.cs`` (older versions):
    ```
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(Configuration.GetConnectionString("DefaultConnection")));
        
        // Other service configurations...
    }
    ```
    This program's `Program.cs` file:
    ```
    // Dynamically choose the provider for multiple providers and their migrations for EF Core
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
    {
        switch (provider)
        {
            case "SqlServer":
                options.UseSqlServer(connectionString);
                break;
            case "Postgresql":
                options.UseNpgsql(supabaseConnectionString);
                break;
            default:
                throw new InvalidOperationException($"Unsupported provider: {provider}");
        }
    });
    ```
   
    In Visual Studio, you can create an SQL server manually in the SQL Server Object Explorer, then you may use that database name instead.
4. **Apply Migrations:**

    After configuring the connection string, apply the migrations to set up the database schema using either the .NET CLI or Package Manager Console:

    **Using .NET CLI:**

    ```
    dotnet ef migrations add InitialCreate
    dotnet ef database update
    ```

    **Using Package Manager Console:**

    ```
    Add-Migration InitialCreate
    Update-Database
    ```

### Installation

1. Clone the repository:
    ```
    git clone https://github.com/JSlush95/StorefrontApp.git
    cd StorefrontAppCore
    ```

2. Restore .NET dependencies:
    ```
    dotnet restore
    ```

3. Build the project:
    ```
    dotnet build
    ```

## Usage

### Bank API Integration/Security
Utilizing asymmetric encryption with a public key and a secretly kept private key, I can keep the data secure while interacting with this banking website web API.

The application integrates with an external banking API for payment processing and refunds. It securely transmits encrypted card and user data to the bank, handles responses, and updates the order status accordingly.

### User Account Management (Account Controller)
The application provides comprehensive user account management features, including registration, login, password recovery, and profile updates. It uses ASP.NET Identity for authentication and authorization, which allows more secure management.

#### Dependencies
The Account Controller uses the following services:
- `SignInManager<User>`
- `UserManager<User>`
- `ILogger<ManageController>`
- `IEmailSender`

**Relevant Functions:**
- **Register**: 
    - Method: POST
    - URL: `/Account/Register`
    - Body: `{ "username": "string", "password": "string", "email": "string" }`
    - **Description**: Registers a new user account with the provided username, password, and email.

- **Login**: 
    - Method: POST
    - URL: `/Account/Login`
    - Body: `{ "username": "string", "password": "string" }`
    - **Description**: Authenticates the user with the provided username and password, creating a session for the user.

- **Logout**: 
    - Method: POST
    - URL: `/Account/LogOff`
    - **Description**: Logs the user out of the application, ending their session.

- **ForgotPassword**:
    - Method: POST
    - URL: `/Account/ForgotPassword`
    - Body: `{ "email": "string" }`
    - **Description**: Initiates the password recovery process by sending a password reset email to the provided email address.

- **ResetPassword**:
    - Method: POST
    - URL: `/Account/ResetPassword`
    - Body: `{ "Email": "string", "Password": "string", "ConfirmPassword": "string", "Code": "string" }`
    - **Description**: Resets the user's password using the provided email, new password, confirmation password, and reset code.

- **SendCode**:
    - Method: POST
    - URL: `/Account/SendCode`
    - Body: `{ "Provider": "string", "ReturnUrl": "string", "RememberMe": true }`
    - **Description**: Sends a verification code for two-factor authentication or other security checks.

### Store Account Management (Manage Controller)
This controller will handle matters related to store accounts, such as payment methods, initiating a transactio via the banking web API, and management of orders and shopping carts.

#### Dependencies
The Manage Controller uses the following services:
- `SignInManager<User>`
- `UserManager<User>`
- `ApplicationDbContext`
- `Cryptography`
- `ILogger<ManageController>`

**Relevant Functions:**
- **CreateStoreAccount:**
    - Method: POST
    - URL: /Manage/CreateStoreAccount
    - Body: `{ "accountTypeInput": "AccountType", "accountAliasInput": "string" }`
    - **Description**: Creates a new store account with the specified type and alias. The account is initialized and stored in the database.

- **SetAccountAlias:**
    - Method: POST
    - URL: /Manage/SetAccountAlias
    - Body: `{ "ChangeAliasInput": "string" }`
    - **Description**: Updates the alias for an existing store account. Ensures that the new alias is unique and valid.

- **AddPaymentMethod:**
    - Method: POST
    - URL: /Manage/AddPaymentMethod
    - Body: `{ "cardNumber": "string", "keyPIN": "string" }`
    - **Description**: Adds a new payment method by encrypting and storing the provided card number and PIN.

- **RemovePaymentMethod:**
    - Method: POST
    - URL: /Manage/RemovePaymentMethod
    - Body: `{ "paymentMethodID": "int" }`
    - **Description**: Removes a payment method identified by the given ID from the user's account.

- **CreateOrder:**
    - Method: POST
    - URL: /Manage/CreateOrder
    - Body: `{ "ShippingAddress": "string", "SelectedPaymentMethodID": "int", "ShoppingCartItems": [{ "ProductID": "int", "Quantity": "int" }] }`
    - **Description**: Creates a new order using the specified shipping address, selected payment method, and items in the shopping cart.

- **RefundOrder:**
    - Method: POST
    - URL: /Manage/RefundOrder
    - Body: `{ "orderID": "int" }`
    - **Description**: Processes a refund for the specified order ID. Updates the order status and account balances accordingly.

- **EnableTwoFactorAuthentication:**
    - Method: POST
    - URL: /Manage/EnableTwoFactorAuthentication
    - **Description**: Enables two-factor authentication for the user’s account, enhancing security.

- **DisableTwoFactorAuthentication:**
    - Method: POST
    - URL: /Manage/DisableTwoFactorAuthentication
    - **Description**: Disables two-factor authentication for the user’s account.

- **ChangeEmail:**
    - Method: POST
    - URL: /Manage/ChangeEmail
    - Body: `{ "OldEmail": "string", "NewEmail": "string" }`
    - **Description**: Changes the email address associated with the user’s account to the new provided email.

- **ChangeUsername:**
    - Method: POST
    - URL: /Manage/ChangeUsername
    - Body: `{ "OldUsername": "string", "NewUsername": "string" }`
    - **Description**: Updates the username for the user’s account, ensuring the new username is unique.

- **ChangePassword:**
    - Method: POST
    - URL: /Manage/ChangePassword
    - Body: `{ "OldPassword": "string", "NewPassword": "string" }`
    - **Description**: Changes the user’s password from the old password to the new password provided.

### Product and Store Displays (Home Controller)
The home controller will allow the user to query for specific subsets of data when needed. The products will display according to the sort and search criteria. A store account is required before the user may use the checkout and shopping cart system.
