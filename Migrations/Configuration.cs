namespace BlogApplication.Migrations
{
    using BlogApplication.Models;
    using Microsoft.AspNet.Identity;
    using Microsoft.AspNet.Identity.EntityFramework;
    using System;
    using System.Data.Entity;
    using System.Data.Entity.Migrations;
    using System.Linq;

    internal sealed class Configuration : DbMigrationsConfiguration<Models.ApplicationDbContext>
    {
        public Configuration()
        {
            AutomaticMigrationsEnabled = true;
        }

        protected override void Seed(Models.ApplicationDbContext context)
        {
            //  This method will be called after migrating to the latest version.

            //  You can use the DbSet<T>.AddOrUpdate() helper extension method 
            //  to avoid creating duplicate seed data.
            var roleManager = new RoleManager<IdentityRole>(new RoleStore<IdentityRole>(context));
            var userManager = new UserManager<ApplicationUser>(new UserStore<ApplicationUser>(context));


            if (!context.Roles.Any(r => r.Name == "Admin"))
            {
                roleManager.Create(new IdentityRole { Name = "Admin" });
            }

            if (!context.Roles.Any(r => r.Name == "Moderator"))
            {
                roleManager.Create(new IdentityRole { Name = "Moderator" });
            }

            ApplicationUser admin = null;
            if (!context.Users.Any(p => p.UserName == "admin@myblogapp.com"))
            {
                admin = new ApplicationUser();
                admin.UserName = "admin@myblogapp.com";
                admin.Email = "admin@myblogapp.com";
                admin.FirstName = "Admin";
                admin.LastName = "User";
                admin.DisplayName = "Admin User";

                userManager.Create(admin, "Password-1");
            }
            else
            {
                admin = context.Users.Where(p => p.UserName == "admin@myblogapp.com")
                    .FirstOrDefault();
            }
            if (!userManager.IsInRole(admin.Id, "Admin"))
            {
                userManager.AddToRole(admin.Id, "Admin");
            }

            ApplicationUser moderator = null;
            if (!context.Users.Any(p => p.UserName == "1035231217@qq.com"))
            {
                moderator = new ApplicationUser();
                moderator.UserName = "1035231217@qq.com";
                moderator.Email = "1035231217@qq.com";
                moderator.FirstName = "Yilin";
                moderator.LastName = "Lu";
                moderator.DisplayName = "Luke";

                userManager.Create(moderator, "Hjkl;456");
            }
            else
            {
                moderator = context.Users.Where(p => p.UserName == "1035231217@qq.com")
                    .FirstOrDefault();
            }
            if (!userManager.IsInRole(moderator.Id, "Moderator"))
            {
                userManager.AddToRole(moderator.Id, "Moderator");
            }
        }
    }
}
