using System;
using System.Collections.Generic;
using Microsoft.Data.Sqlite;

class Program
{
    static void Main(string[] args)
    {
        string connectionString = "Data Source=tesla_rental.db";

        try
        {
            var rentalCtrl = new TeslaRentalCtrl(connectionString);

            while (true)
            {
                Console.WriteLine("Choose action: 'add_car', 'add_client', 'rent_car', 'print' or 'stop'.");
                var userCommand = Console.ReadLine();

                switch (userCommand)
                {
                    case "add_car":
                        rentalCtrl.AddCar();
                        break;
                    case "add_client":
                        rentalCtrl.AddClient();
                        break;
                    case "rent_car":
                        rentalCtrl.RentCar();
                        break;
                    case "print":
                        rentalCtrl.PrintRentals();
                        break;
                    case "stop":
                        return;
                    default:
                        Console.WriteLine("Invalid action");
                        break;
                }
            }

        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }

    public class TeslaRentalCtrl
    {
        private readonly string connectionString;

        public TeslaRentalCtrl(string connectionString)
        {
            this.connectionString = connectionString;
            CreateTables();
        }

        public void AddCar()
        {
            Console.WriteLine("Enter Tesla Model:");
            string model = Console.ReadLine();

            Console.WriteLine("Enter Hourly Rate (EUR/h):");
            double hourlyRate = Convert.ToDouble(Console.ReadLine());

            Console.WriteLine("Enter Km Rate (EUR/km):");
            double kmRate = Convert.ToDouble(Console.ReadLine());

            AddCarToTable(model, hourlyRate, kmRate);
        }

        public void AddClient()
        {
            Console.WriteLine("Enter Client Name:");
            string name = Console.ReadLine();

            Console.WriteLine("Enter Client Email:");
            string email = Console.ReadLine();

            AddClientToTable(name, email);
        }

        public void RentCar()
        {
            Console.WriteLine("Enter Client ID:");
            int clientId = Convert.ToInt32(Console.ReadLine());

            Console.WriteLine("Enter Car ID:");
            int carId = Convert.ToInt32(Console.ReadLine());

            Console.WriteLine("Enter Start Time (yyyy-MM-dd HH:mm):");
            DateTime startTime = DateTime.Parse(Console.ReadLine());

            Console.WriteLine("Enter End Time (yyyy-MM-dd HH:mm):");
            DateTime endTime = DateTime.Parse(Console.ReadLine());

            Console.WriteLine("Enter Km Driven:");
            double kmDriven = Convert.ToDouble(Console.ReadLine());

            RentCarInTable(clientId, carId, startTime, endTime, kmDriven);
        }

        public void PrintRentals()
        {
            using (var reader = GetAllRentalsFromTable())
            {
                Console.WriteLine("Rental Records:");
                while (reader.Read())
                {
                    Console.WriteLine($"Rental ID: {reader["ID"]}, Client ID: {reader["ClientID"]}, Car ID: {reader["CarID"]}, Total Payment: EUR {reader["TotalPayment"]}");
                }
            }
        }

        private void CreateTables()
        {
            using (var connection = new SqliteConnection(connectionString))
            {
                connection.Open();

                var createCarsTable = @"CREATE TABLE IF NOT EXISTS Cars (
                                            ID INTEGER PRIMARY KEY AUTOINCREMENT,
                                            Model TEXT NOT NULL,
                                            HourlyRate REAL NOT NULL,
                                            KmRate REAL NOT NULL
                                        );";
                var createClientsTable = @"CREATE TABLE IF NOT EXISTS Clients (
                                                ID INTEGER PRIMARY KEY AUTOINCREMENT,
                                                Name TEXT NOT NULL,
                                                Email TEXT NOT NULL
                                            );";
                var createRentalsTable = @"CREATE TABLE IF NOT EXISTS Rentals (
                                                ID INTEGER PRIMARY KEY AUTOINCREMENT,
                                                ClientID INTEGER NOT NULL,
                                                CarID INTEGER NOT NULL,
                                                StartTime TEXT NOT NULL,
                                                EndTime TEXT NOT NULL,
                                                KmDriven REAL NOT NULL,
                                                TotalPayment REAL NOT NULL,
                                                FOREIGN KEY(ClientID) REFERENCES Clients(ID),
                                                FOREIGN KEY(CarID) REFERENCES Cars(ID)
                                            );";

                ExecuteNonQuery(connection, createCarsTable);
                ExecuteNonQuery(connection, createClientsTable);
                ExecuteNonQuery(connection, createRentalsTable);
            }
        }

        private void AddCarToTable(string model, double hourlyRate, double kmRate)
        {
            using (var connection = new SqliteConnection(connectionString))
            {
                connection.Open();
                var insertCmd = connection.CreateCommand();
                insertCmd.CommandText = "INSERT INTO Cars (Model, HourlyRate, KmRate) VALUES (@model, @hourlyRate, @kmRate);";
                insertCmd.Parameters.AddWithValue("@model", model);
                insertCmd.Parameters.AddWithValue("@hourlyRate", hourlyRate);
                insertCmd.Parameters.AddWithValue("@kmRate", kmRate);
                insertCmd.ExecuteNonQuery();
            }
        }

        private void AddClientToTable(string name, string email)
        {
            using (var connection = new SqliteConnection(connectionString))
            {
                connection.Open();
                var insertCmd = connection.CreateCommand();
                insertCmd.CommandText = "INSERT INTO Clients (Name, Email) VALUES (@name, @email);";
                insertCmd.Parameters.AddWithValue("@name", name);
                insertCmd.Parameters.AddWithValue("@email", email);
                insertCmd.ExecuteNonQuery();
            }
        }

        private void RentCarInTable(int clientId, int carId, DateTime startTime, DateTime endTime, double kmDriven)
        {
            using (var connection = new SqliteConnection(connectionString))
            {
                connection.Open();

                var getCarCmd = connection.CreateCommand();
                getCarCmd.CommandText = "SELECT HourlyRate, KmRate FROM Cars WHERE ID = @carId;";
                getCarCmd.Parameters.AddWithValue("@carId", carId);

                using (var reader = getCarCmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        double hourlyRate = reader.GetDouble(0);
                        double kmRate = reader.GetDouble(1);

                        double hours = (endTime - startTime).TotalHours;
                        double totalPayment = (hours * hourlyRate) + (kmDriven * kmRate);

                        var insertRentalCmd = connection.CreateCommand();
                        insertRentalCmd.CommandText = @"INSERT INTO Rentals (ClientID, CarID, StartTime, EndTime, KmDriven, TotalPayment) 
                                                      VALUES (@clientId, @carId, @startTime, @endTime, @kmDriven, @totalPayment);";
                        insertRentalCmd.Parameters.AddWithValue("@clientId", clientId);
                        insertRentalCmd.Parameters.AddWithValue("@carId", carId);
                        insertRentalCmd.Parameters.AddWithValue("@startTime", startTime.ToString("o"));
                        insertRentalCmd.Parameters.AddWithValue("@endTime", endTime.ToString("o"));
                        insertRentalCmd.Parameters.AddWithValue("@kmDriven", kmDriven);
                        insertRentalCmd.Parameters.AddWithValue("@totalPayment", totalPayment);

                        insertRentalCmd.ExecuteNonQuery();

                        Console.WriteLine($"Rental recorded. Total Payment: EUR {totalPayment:F2}");
                    }
                    else
                    {
                        Console.WriteLine("Car not found.");
                    }
                }
            }
        }

        private SqliteDataReader GetAllRentalsFromTable()
        {
            var connection = new SqliteConnection(connectionString);
            connection.Open();

            var selectCmd = connection.CreateCommand();
            selectCmd.CommandText = "SELECT * FROM Rentals;";

            return selectCmd.ExecuteReader();
        }

        private void ExecuteNonQuery(SqliteConnection connection, string query)
        {
            var command = connection.CreateCommand();
            command.CommandText = query;
            command.ExecuteNonQuery();
        }
    }
}
