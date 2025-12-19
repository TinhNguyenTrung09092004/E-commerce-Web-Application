# WebShop - E-commerce Application

A full-featured e-commerce web application built with ASP.NET Core 8.0 MVC, featuring product management, shopping cart, order processing, and admin dashboard.

## Features

### Customer Features
- User registration and authentication with email confirmation
- Browse products by categories and brands
- Product search and filtering
- Shopping cart functionality
- Order placement and tracking
- Product reviews and ratings
- Product chat/Q&A system
- Voucher/discount code support
- User profile management
- Password reset functionality

### Admin Features
- Admin dashboard with statistics
- Product management (CRUD operations)
- Category and brand management
- Order management and status updates
- Banner management for homepage
- Voucher management
- User management

## Technologies Used

- **Framework**: ASP.NET Core 8.0 MVC
- **Database**: SQL Server with Entity Framework Core 8.0
- **Authentication**: ASP.NET Core Identity
- **Email Service**: MailKit (Gmail SMTP)
- **UI Components**: X.PagedList for pagination
- **Development Tools**: Visual Studio Code / Visual Studio 2022

## Prerequisites

Before running this application, ensure you have the following installed:

- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [SQL Server](https://www.microsoft.com/sql-server/sql-server-downloads) (LocalDB, Express, or full version)
- A code editor (Visual Studio 2022 or Visual Studio Code recommended)

## Installation & Setup

### 1. Clone the Repository

```bash
git clone <repository-url>
cd WebShop
```

### 2. Configure Database Connection

Open `appsettings.json` and update the connection string to match your SQL Server instance:

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=localhost;Database=Sales_and_Admin_website;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True;"
}
```

**Note**: Modify `Server=localhost` if your SQL Server is on a different host.

### 3. Configure Email Settings

Update the SMTP settings in `appsettings.json` for email functionality:

```json
"SmtpSettings": {
  "Server": "smtp.gmail.com",
  "Port": 587,
  "SenderName": "WebShop",
  "SenderEmail": "your-email@gmail.com",
  "Username": "your-email@gmail.com",
  "Password": "your-app-specific-password"
}
```

**Important**:
- Replace `your-email@gmail.com` with your Gmail address
- For the password, use a Gmail App Password (not your regular password)
- To create an App Password: Go to Google Account > Security > 2-Step Verification > App passwords

### 4. Restore Dependencies

```bash
dotnet restore
```

### 5. Apply Database Migrations

Create and update the database with the following commands:

```bash
dotnet ef database update
```

If migrations are not present, create them:

```bash
dotnet ef migrations add InitialCreate
dotnet ef database update
```

### 6. Create Admin Account

Run the application with the `create-admin` command to set up an administrator account:

```bash
dotnet run create-admin
```

Follow the prompts to enter:
- Admin email address
- Admin password (must meet security requirements: uppercase, lowercase, number, special character)

### 7. Run the Application

```bash
dotnet run
```

The application will start and be available at:
- HTTPS: `https://localhost:5001`
- HTTP: `http://localhost:5000`

## Usage Guide

### First Time Setup

1. **Start the application** using `dotnet run`
2. **Create admin account** using `dotnet run create-admin` (if not already done)
3. **Access the application** in your web browser
4. **Login as admin** to configure:
   - Add product categories
   - Add brands
   - Upload products
   - Create banners for homepage
   - Set up vouchers

### Customer Workflow

1. **Register an account** via the registration page
2. **Check email** for confirmation link and verify your account
3. **Login** with verified credentials
4. **Browse products** on the homepage or by category
5. **Add products to cart** and proceed to checkout
6. **Place orders** with optional voucher codes
7. **Track orders** in your profile
8. **Leave reviews** on purchased products

### Admin Workflow

1. **Login** with admin credentials
2. **Access Admin Dashboard** from the navigation menu
3. **Manage Products**:
   - Add new products with images
   - Edit existing products
   - Update stock levels
   - Set featured products
4. **Manage Orders**:
   - View all orders
   - Update order status (Pending, Processing, Shipped, Delivered)
   - Update payment status
5. **Manage Categories & Brands**:
   - Create product categories
   - Add brand information
6. **Manage Banners**:
   - Upload homepage banners
   - Set banner display order

## Project Structure

```
WebShop/
├── Controllers/          # MVC Controllers (Account, Admin, Home, User)
├── Models/              # Data models and view models
├── Views/               # Razor view templates
├── Services/            # Business logic services (Email, etc.)
├── Commands/            # CLI commands (CreateAdmin)
├── Data/                # Database context and configurations
├── Migrations/          # Entity Framework migrations
├── wwwroot/            # Static files (CSS, JS, images)
│   ├── css/
│   ├── js/
│   └── uploads/        # User-uploaded images
├── appsettings.json    # Application configuration
└── Program.cs          # Application entry point
```

## Configuration Files

### appsettings.json
Main configuration file containing:
- Database connection string
- SMTP email settings
- Logging configuration
- Allowed hosts

### appsettings.Development.json
Development-specific settings (optional overrides)

## Troubleshooting

### Database Connection Issues
- Verify SQL Server is running
- Check connection string in `appsettings.json`
- Ensure database exists or run `dotnet ef database update`

### Email Not Sending
- Verify SMTP settings in `appsettings.json`
- Use Gmail App Password, not regular password
- Enable "Less secure app access" if using older Gmail security
- Check firewall settings for port 587

### Migration Errors
```bash
# Reset migrations
dotnet ef database drop
dotnet ef migrations remove
dotnet ef migrations add InitialCreate
dotnet ef database update
```

### Admin Creation Fails
- Ensure database is created and migrations are applied
- Password must meet requirements: min 6 characters, uppercase, lowercase, number, special character

## Security Notes

**Before Deployment**:
1. Change all default passwords in `appsettings.json`
2. Use environment variables for sensitive data
3. Enable HTTPS in production
4. Update connection strings for production database
5. Configure proper CORS settings
6. Set `RequireConfirmedAccount = true` for email verification

## Database Schema

Main entities:
- **ApplicationUser**: Extended Identity user with profile fields
- **Product**: Product information with pricing and stock
- **Category**: Product categories
- **Brand**: Product brands
- **Order**: Customer orders
- **OrderItem**: Items in orders
- **CartItem**: Shopping cart items
- **ProductReview**: Customer reviews
- **Voucher**: Discount codes
- **Banner**: Homepage banners

## Commands

### Development
```bash
dotnet run                    # Run application
dotnet watch run             # Run with hot reload
dotnet build                 # Build project
dotnet test                  # Run tests (if available)
```

### Database
```bash
dotnet ef migrations add <Name>    # Create migration
dotnet ef database update          # Apply migrations
dotnet ef database drop            # Drop database
```

### Admin
```bash
dotnet run create-admin      # Create admin account
```

## License

This project is for educational purposes (University Final Project).

## Support

For issues or questions, please contact the development team or create an issue in the repository.

## Contributors

Developed as part of Net Technology course, Semester 2, Three University.