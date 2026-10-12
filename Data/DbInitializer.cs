using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using AcxiomCRM.Models;

namespace AcxiomCRM.Data
{
    public static class DbInitializer
    {
        public static async Task SeedAsync(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

            // Ensure database is created
            await context.Database.EnsureCreatedAsync();

            // Seed Roles
            string[] roleNames = { "Admin", "Manager", "SalesExecutive" };
            foreach (var roleName in roleNames)
            {
                if (!await roleManager.RoleExistsAsync(roleName))
                {
                    await roleManager.CreateAsync(new IdentityRole(roleName));
                }
            }

            // Seed Admin User
            var adminEmail = "admin@acxiomcrm.com";
            var adminUser = await userManager.FindByEmailAsync(adminEmail);
            if (adminUser == null)
            {
                adminUser = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    FullName = "Alex Morgan (System Admin)",
                    EmailConfirmed = true,
                    IsActive = true,
                    CreatedDate = DateTime.UtcNow.AddMonths(-3)
                };
                var result = await userManager.CreateAsync(adminUser, "Admin@123");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(adminUser, "Admin");
                }
            }

            // Seed Manager User
            var managerEmail = "manager@acxiomcrm.com";
            var managerUser = await userManager.FindByEmailAsync(managerEmail);
            if (managerUser == null)
            {
                managerUser = new ApplicationUser
                {
                    UserName = managerEmail,
                    Email = managerEmail,
                    FullName = "Sarah Jenkins (Sales Manager)",
                    EmailConfirmed = true,
                    IsActive = true,
                    CreatedDate = DateTime.UtcNow.AddMonths(-3)
                };
                var result = await userManager.CreateAsync(managerUser, "Manager@123");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(managerUser, "Manager");
                }
            }

            // Seed Sales Executive 1
            var sales1Email = "sales@acxiomcrm.com";
            var sales1User = await userManager.FindByEmailAsync(sales1Email);
            if (sales1User == null)
            {
                sales1User = new ApplicationUser
                {
                    UserName = sales1Email,
                    Email = sales1Email,
                    FullName = "Rahul Sharma (Sales Executive)",
                    EmailConfirmed = true,
                    IsActive = true,
                    CreatedDate = DateTime.UtcNow.AddMonths(-2)
                };
                var result = await userManager.CreateAsync(sales1User, "Sales@123");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(sales1User, "SalesExecutive");
                }
            }

            // Seed Sales Executive 2
            var sales2Email = "sales2@acxiomcrm.com";
            var sales2User = await userManager.FindByEmailAsync(sales2Email);
            if (sales2User == null)
            {
                sales2User = new ApplicationUser
                {
                    UserName = sales2Email,
                    Email = sales2Email,
                    FullName = "Priya Patel (Sales Executive)",
                    EmailConfirmed = true,
                    IsActive = true,
                    CreatedDate = DateTime.UtcNow.AddMonths(-2)
                };
                var result = await userManager.CreateAsync(sales2User, "Sales@123");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(sales2User, "SalesExecutive");
                }
            }

            // Seed Customers if empty
            if (!await context.Customers.AnyAsync())
            {
                var customers = new List<Customer>
                {
                    new Customer
                    {
                        CustomerCode = "CUST-1001",
                        CustomerName = "Apex Solutions Inc.",
                        Email = "contact@apexsolutions.com",
                        Phone = "9876543210",
                        CompanyName = "Apex Solutions Inc.",
                        Address = "104 Tech Boulevard",
                        City = "Bangalore",
                        State = "Karnataka",
                        Status = "Active",
                        CreatedDate = DateTime.UtcNow.AddMonths(-2),
                        CreatedBy = adminUser.Id,
                        AssignedToId = sales1User.Id
                    },
                    new Customer
                    {
                        CustomerCode = "CUST-1002",
                        CustomerName = "BlueHorizon Retail",
                        Email = "info@bluehorizon.io",
                        Phone = "9123456780",
                        CompanyName = "BlueHorizon Retail Pvt Ltd",
                        Address = "45 Commercial Street",
                        City = "Mumbai",
                        State = "Maharashtra",
                        Status = "Active",
                        CreatedDate = DateTime.UtcNow.AddMonths(-1),
                        CreatedBy = managerUser.Id,
                        AssignedToId = sales2User.Id
                    },
                    new Customer
                    {
                        CustomerCode = "CUST-1003",
                        CustomerName = "CloudFront Logistics",
                        Email = "sales@cloudfrontlog.com",
                        Phone = "9988776655",
                        CompanyName = "CloudFront Logistics",
                        Address = "78 Port Road",
                        City = "Chennai",
                        State = "Tamil Nadu",
                        Status = "Active",
                        CreatedDate = DateTime.UtcNow.AddDays(-20),
                        CreatedBy = sales1User.Id,
                        AssignedToId = sales1User.Id
                    },
                    new Customer
                    {
                        CustomerCode = "CUST-1004",
                        CustomerName = "Delta Dynamics",
                        Email = "procure@deltadynamics.com",
                        Phone = "9811223344",
                        CompanyName = "Delta Dynamics Corp",
                        Address = "12 Cyber City",
                        City = "Gurugram",
                        State = "Haryana",
                        Status = "Active",
                        CreatedDate = DateTime.UtcNow.AddDays(-10),
                        CreatedBy = sales2User.Id,
                        AssignedToId = sales2User.Id
                    }
                };

                context.Customers.AddRange(customers);
                await context.SaveChangesAsync();
            }

            // Seed Leads if empty
            if (!await context.Leads.AnyAsync())
            {
                var leads = new List<Lead>
                {
                    new Lead
                    {
                        LeadCode = "LEAD-1001",
                        LeadName = "Vikram Aditya",
                        Email = "vikram.aditya@zenithcorp.com",
                        Phone = "9845012345",
                        CompanyName = "Zenith Corp",
                        Source = "Website",
                        Status = "Qualified",
                        Priority = "High",
                        ExpectedValue = 45000m,
                        CreatedDate = DateTime.UtcNow.AddDays(-15),
                        AssignedToId = sales1User.Id
                    },
                    new Lead
                    {
                        LeadCode = "LEAD-1002",
                        LeadName = "Ananya Roy",
                        Email = "ananya.roy@stellarfin.com",
                        Phone = "9731298765",
                        CompanyName = "Stellar Financial",
                        Source = "Referral",
                        Status = "Contacted",
                        Priority = "Medium",
                        ExpectedValue = 28000m,
                        CreatedDate = DateTime.UtcNow.AddDays(-10),
                        AssignedToId = sales2User.Id
                    },
                    new Lead
                    {
                        LeadCode = "LEAD-1003",
                        LeadName = "Marcus Vance",
                        Email = "marcus@vancetech.com",
                        Phone = "9820011223",
                        CompanyName = "Vance Technologies",
                        Source = "Event",
                        Status = "New",
                        Priority = "High",
                        ExpectedValue = 65000m,
                        CreatedDate = DateTime.UtcNow.AddDays(-3),
                        AssignedToId = sales1User.Id
                    },
                    new Lead
                    {
                        LeadCode = "LEAD-1004",
                        LeadName = "Kavita Rao",
                        Email = "kavita@novasystems.net",
                        Phone = "9880099887",
                        CompanyName = "Nova Systems",
                        Source = "Social Media",
                        Status = "Lost",
                        Priority = "Low",
                        ExpectedValue = 12000m,
                        CreatedDate = DateTime.UtcNow.AddDays(-25),
                        AssignedToId = sales2User.Id
                    }
                };

                context.Leads.AddRange(leads);
                await context.SaveChangesAsync();
            }

            // Seed Opportunities if empty
            if (!await context.Opportunities.AnyAsync())
            {
                var apexCustomer = await context.Customers.FirstOrDefaultAsync(c => c.CustomerCode == "CUST-1001");
                var blueCustomer = await context.Customers.FirstOrDefaultAsync(c => c.CustomerCode == "CUST-1002");
                var cloudCustomer = await context.Customers.FirstOrDefaultAsync(c => c.CustomerCode == "CUST-1003");

                if (apexCustomer != null && blueCustomer != null && cloudCustomer != null)
                {
                    var opportunities = new List<Opportunity>
                    {
                        new Opportunity
                        {
                            OpportunityName = "Apex Enterprise Cloud Migration",
                            CustomerId = apexCustomer.CustomerId,
                            Amount = 85000m,
                            Stage = "Negotiation",
                            Probability = 80,
                            ExpectedCloseDate = DateTime.UtcNow.AddDays(14),
                            Status = "Open",
                            CreatedDate = DateTime.UtcNow.AddDays(-20),
                            AssignedToId = sales1User.Id,
                            Notes = "Final contract terms being reviewed by legal team."
                        },
                        new Opportunity
                        {
                            OpportunityName = "BlueHorizon Omnichannel POS Upgrade",
                            CustomerId = blueCustomer.CustomerId,
                            Amount = 52000m,
                            Stage = "Proposal",
                            Probability = 50,
                            ExpectedCloseDate = DateTime.UtcNow.AddDays(25),
                            Status = "Open",
                            CreatedDate = DateTime.UtcNow.AddDays(-12),
                            AssignedToId = sales2User.Id,
                            Notes = "Demo completed; technical proposal sent to CTO."
                        },
                        new Opportunity
                        {
                            OpportunityName = "CloudFront Fleet Telematics Integration",
                            CustomerId = cloudCustomer.CustomerId,
                            Amount = 120000m,
                            Stage = "Won",
                            Probability = 100,
                            ExpectedCloseDate = DateTime.UtcNow.AddDays(-5),
                            Status = "Won",
                            CreatedDate = DateTime.UtcNow.AddMonths(-1),
                            AssignedToId = sales1User.Id,
                            Notes = "Deal closed successfully. Implementation scheduled for next quarter."
                        },
                        new Opportunity
                        {
                            OpportunityName = "Delta Dynamics Analytics Pilot",
                            CustomerId = apexCustomer.CustomerId,
                            Amount = 30000m,
                            Stage = "Qualification",
                            Probability = 20,
                            ExpectedCloseDate = DateTime.UtcNow.AddDays(45),
                            Status = "Open",
                            CreatedDate = DateTime.UtcNow.AddDays(-5),
                            AssignedToId = sales2User.Id,
                            Notes = "Initial scoping call done. Scheduling architecture review."
                        }
                    };

                    context.Opportunities.AddRange(opportunities);
                    await context.SaveChangesAsync();
                }
            }

            // Seed FollowUps if empty
            if (!await context.FollowUps.AnyAsync())
            {
                var customer = await context.Customers.FirstOrDefaultAsync();
                var lead = await context.Leads.FirstOrDefaultAsync();
                var opp = await context.Opportunities.FirstOrDefaultAsync();

                if (customer != null && lead != null && opp != null)
                {
                    var followUps = new List<FollowUp>
                    {
                        new FollowUp
                        {
                            CustomerId = customer.CustomerId,
                            FollowUpDate = DateTime.UtcNow.AddDays(2),
                            FollowUpType = "Meeting",
                            Subject = "Executive review meeting on contract SLA",
                            Remarks = "Review clause 4.2 with VP Operations.",
                            Status = "Planned",
                            AssignedToId = sales1User.Id,
                            CreatedDate = DateTime.UtcNow.AddDays(-1)
                        },
                        new FollowUp
                        {
                            LeadId = lead.LeadId,
                            FollowUpDate = DateTime.UtcNow.AddDays(1),
                            FollowUpType = "Call",
                            Subject = "Discovery call regarding budget timeline",
                            Remarks = "Clarify Q3 deployment prerequisites.",
                            Status = "Planned",
                            AssignedToId = sales1User.Id,
                            CreatedDate = DateTime.UtcNow.AddDays(-2)
                        },
                        new FollowUp
                        {
                            OpportunityId = opp.OpportunityId,
                            FollowUpDate = DateTime.UtcNow.AddDays(-1),
                            FollowUpType = "Email",
                            Subject = "Send revised commercial rate card",
                            Remarks = "Commercial rates sent as requested.",
                            Status = "Completed",
                            AssignedToId = sales1User.Id,
                            CreatedDate = DateTime.UtcNow.AddDays(-4)
                        }
                    };

                    context.FollowUps.AddRange(followUps);
                    await context.SaveChangesAsync();
                }
            }

            // Seed Activities if empty
            if (!await context.Activities.AnyAsync())
            {
                var customer = await context.Customers.FirstOrDefaultAsync();
                if (customer != null)
                {
                    var activities = new List<Activity>
                    {
                        new Activity
                        {
                            CustomerId = customer.CustomerId,
                            ActivityType = "Call",
                            Subject = "Quarterly Business Review check-in",
                            Description = "Discussed customer satisfaction score and expansion roadmap.",
                            ActivityDate = DateTime.UtcNow.AddDays(-4),
                            Status = "Completed",
                            AssignedToId = sales1User.Id
                        },
                        new Activity
                        {
                            CustomerId = customer.CustomerId,
                            ActivityType = "Meeting",
                            Subject = "Product Demo for APAC Team",
                            Description = "Demonstrated analytics module and custom dashboards.",
                            ActivityDate = DateTime.UtcNow.AddDays(-2),
                            Status = "Completed",
                            AssignedToId = sales2User.Id
                        }
                    };

                    context.Activities.AddRange(activities);
                    await context.SaveChangesAsync();
                }
            }

            // Seed initial Audit Logs if empty
            if (!await context.AuditLogs.AnyAsync())
            {
                var logs = new List<AuditLog>
                {
                    new AuditLog
                    {
                        UserId = adminUser.Id,
                        UserName = adminUser.Email!,
                        Action = "Login",
                        EntityName = "Authentication",
                        RecordId = null,
                        OldValue = null,
                        NewValue = "Admin logged in successfully",
                        CreatedDate = DateTime.UtcNow.AddDays(-1),
                        IpAddress = "127.0.0.1"
                    },
                    new AuditLog
                    {
                        UserId = adminUser.Id,
                        UserName = adminUser.Email!,
                        Action = "Create",
                        EntityName = "Customer",
                        RecordId = "CUST-1001",
                        OldValue = null,
                        NewValue = "Created customer Apex Solutions Inc.",
                        CreatedDate = DateTime.UtcNow.AddDays(-1),
                        IpAddress = "127.0.0.1"
                    }
                };

                context.AuditLogs.AddRange(logs);
                await context.SaveChangesAsync();
            }
        }
    }
}
