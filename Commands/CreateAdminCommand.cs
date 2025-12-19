using Microsoft.AspNetCore.Identity;
using WebShop.Models;

namespace WebShop.Commands
{
    public class CreateAdminCommand
    {
        private readonly IServiceProvider _serviceProvider;

        public CreateAdminCommand(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public async Task ExecuteAsync(string[] args)
        {
            var userManager = _serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = _serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();

            string email = null;
            string password = null;
            string fullName = "Admin User";

            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "--email" && i + 1 < args.Length)
                {
                    email = args[i + 1];
                }
                else if (args[i] == "--password" && i + 1 < args.Length)
                {
                    password = args[i + 1];
                }
                else if (args[i] == "--fullName" && i + 1 < args.Length)
                {
                    fullName = args[i + 1];
                }
            }

            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                Console.WriteLine("Usage: dotnet run -- create-admin --email <email> --password <password> [--fullName <fullName>]");
                return;
            }

            if (!await roleManager.RoleExistsAsync("Admin"))
            {
                await roleManager.CreateAsync(new IdentityRole("Admin"));
            }

            if (!await roleManager.RoleExistsAsync("User"))
            {
                await roleManager.CreateAsync(new IdentityRole("User"));
            }

            var user = await userManager.FindByEmailAsync(email);
            if (user == null)
            {
                user = new ApplicationUser { UserName = email, Email = email, EmailConfirmed = true, FullName = fullName };
                var result = await userManager.CreateAsync(user, password);

                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(user, "Admin");
                    Console.WriteLine($"Admin user {email} created successfully.");
                }
                else
                {
                    Console.WriteLine("Error creating admin user: " + string.Join(", ", result.Errors.Select(e => e.Description)));
                }
            }
            else
            {
                Console.WriteLine($"User {email} already exists.");
            }
        }
    }
}