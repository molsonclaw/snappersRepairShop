using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SnappersRepairShop.Shared.Models;

namespace SnappersRepairShop.Data;

public static class DbSeeder
{
    public static async Task SeedSampleData(ApplicationDbContext context)
    {
        // Check if we already have customers
        if (await context.Customers.AnyAsync())
        {
            return; // Already seeded
        }

        // Add sample customers
        var customers = new List<Customer>
        {
            new Customer
            {
                FirstName = "John",
                LastName = "Smith",
                Phone = "555-0101",
                Email = "john.smith@example.com",
                Address = "123 Main St, Springfield",
                CreatedDate = DateTime.UtcNow
            },
            new Customer
            {
                FirstName = "Sarah",
                LastName = "Johnson",
                Phone = "555-0102",
                Email = "sarah.j@example.com",
                Address = "456 Oak Ave, Springfield",
                CreatedDate = DateTime.UtcNow
            },
            new Customer
            {
                FirstName = "Mike",
                LastName = "Williams",
                Phone = "555-0103",
                Email = "mike.w@example.com",
                Address = "789 Pine Rd, Springfield",
                CreatedDate = DateTime.UtcNow
            }
        };

        context.Customers.AddRange(customers);
        await context.SaveChangesAsync();

        // Add sample vehicles
        var vehicles = new List<Vehicle>
        {
            new Vehicle
            {
                CustomerId = customers[0].CustomerId,
                Year = 2018,
                Make = "Toyota",
                Model = "Camry",
                VIN = "1HGBH41JXMN109186",
                LicensePlate = "ABC123",
                Color = "Silver",
                CreatedDate = DateTime.UtcNow
            },
            new Vehicle
            {
                CustomerId = customers[0].CustomerId,
                Year = 2020,
                Make = "Honda",
                Model = "Civic",
                VIN = "2HGFC2F59LH123456",
                LicensePlate = "XYZ789",
                Color = "Blue",
                CreatedDate = DateTime.UtcNow
            },
            new Vehicle
            {
                CustomerId = customers[1].CustomerId,
                Year = 2019,
                Make = "Ford",
                Model = "F-150",
                VIN = "1FTFW1ET5KFA12345",
                LicensePlate = "DEF456",
                Color = "Red",
                CreatedDate = DateTime.UtcNow
            },
            new Vehicle
            {
                CustomerId = customers[2].CustomerId,
                Year = 2021,
                Make = "Chevrolet",
                Model = "Silverado",
                VIN = "1GCUYEED5MZ123456",
                LicensePlate = "GHI789",
                Color = "Black",
                CreatedDate = DateTime.UtcNow
            }
        };

        context.Vehicles.AddRange(vehicles);
        await context.SaveChangesAsync();
    }
}

