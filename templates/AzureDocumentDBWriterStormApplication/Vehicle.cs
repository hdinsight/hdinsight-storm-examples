using Microsoft.SCP;
using System;
using System.Collections.Generic;
using System.Text;

namespace AzureDocumentDBWriterStormApplication
{
    /// <summary>
    /// A serializable class that represents a vehicle record
    /// </summary>
    [Serializable]
    public class Vehicle
    {
        public string VIN { get; set; }
        public DateTime Timestamp { get; set; }
        public string Make { get; set; }
        public string Model { get; set; }
        public int Year { get; set; }
        public int Odometer { get; set; }
        public string Status { get; set; }

        static List<string> VehicleMakes = new List<string>() { "AUDI", "BMW", "HONDA", "NISSAN", "TOYOTA" };
        static List<string> VehicleModels = new List<string>() { "Q5", "X5", "PILOT", "MURANO", "HIGHLANDER" };
        static List<string> VehicleStatuses = new List<string>() { "PERFECT", "GOOD", "BAD", "REPAIR" };

        /// <summary>
        /// Generate a random VIN for the Vehicle
        /// </summary>
        /// <param name="seqId">seqId ensures that we always emit same VINs in same sequence</param>
        /// <returns></returns>
        static string GetRandomVIN(long seqId)
        {
            var vin = new StringBuilder();
            for (int i = 0; i < 17; i++)
            {
                vin.Append(seqId);
            }
            return vin.ToString().Substring(0, 17);
        }

        /// <summary>
        /// Based on seqId return repeatable records but with newer timestamps
        /// </summary>
        /// <param name="seqId">The seqId of the tuple in the spout</param>
        /// <returns></returns>
        public static Vehicle GetRandomVehicle(long seqId)
        {
            var vin = GetRandomVIN(seqId);
            var make = (int)(seqId % VehicleMakes.Count);
            var model = (int)(seqId % VehicleModels.Count);
            var year = 2010 + (int)((seqId % (VehicleMakes.Count * 5)) % 5);
            var odometer = int.Parse(vin.Substring(0, 5));
            var status = (int)((seqId % (VehicleMakes.Count * VehicleStatuses.Count)) % VehicleStatuses.Count);

            var vehicle = new Vehicle()
            {
                Timestamp = DateTime.UtcNow,
                VIN = vin,
                Make = VehicleMakes[make],
                Model = VehicleModels[model],
                Year = year,
                Odometer = odometer,
                Status = VehicleStatuses[status]
            };
            return vehicle;
        }

        /// <summary>
        /// Handy method to return the fields in Vehicle as a list
        /// </summary>
        /// <returns></returns>
        public Values GetValues()
        {
            return new Values(
                this.VIN, //Keep VIN as the first item in list to be used as ID in upstream bolts like HBase
                this.Timestamp,
                this.Make,
                this.Model,
                this.Year,
                this.Odometer,
                this.Status);
        }

        /// <summary>
        /// Reverse conversion i.e. from List to vehicle
        /// </summary>
        /// <param name="values"></param>
        /// <returns></returns>
        public Vehicle GetVehicle(List<object> values)
        {
            //TODO: You can add your own validations here to see if the list is indeed a vehicle
            return new Vehicle()
            {
                VIN = (string)values[0], //Keep VIN as the first item in list to be used as ID in upstream bolts like HBase
                Timestamp = (DateTime)values[1],
                Make = (string)values[2],
                Model = (string)values[3],
                Year = (int)values[4],
                Odometer = (int)values[5],
                Status = (string)values[6]
            };
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append("VIN=" + this.VIN);
            sb.Append(",Timestamp=" + this.Timestamp);
            sb.Append(",Make=" + this.Make);
            sb.Append(",Model=" + this.Model);
            sb.Append(",Year=" + this.Year);
            sb.Append(",Odometer=" + this.Odometer);
            sb.Append(",Status=" + this.Status);
            return sb.ToString();
        }
    }
}
