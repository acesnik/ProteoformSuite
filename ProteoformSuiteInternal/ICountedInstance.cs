using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProteoformSuiteInternal
{
    /// <summary>
    /// Uses an static instance counter to set a Unique_ID variable (e.g. private static long instance_counter; //incremented in every constructor with Unique_ID = ++instance_counter; ) 
    /// </summary>
    interface ICountedInstance
    {
        long Unique_ID { get; }
    }
}
