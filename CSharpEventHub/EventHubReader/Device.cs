using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage.Table;

namespace EventHubReader
{
    /// <summary>
    /// Defines the entity format for Table Storage
    /// </summary>
    class Device : TableEntity
    {
        public int value { get; set; }

        public Device() { }
        /// <summary>
        /// Create a new instance of the Device entity
        /// </summary>
        /// <param name="id"></param>
        public Device(int id)
        {
            //Set partition key to the device ID
            this.PartitionKey = id.ToString();
            //Set the unique row key to a guid
            this.RowKey = System.Guid.NewGuid().ToString();
        }
    }
}
