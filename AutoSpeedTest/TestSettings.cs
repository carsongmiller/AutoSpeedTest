using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoSpeedTest
{
	public class TestSettings
	{
		/// <summary>
		/// Run the test infinitely, until stopped? (ignore trials)
		/// </summary>
		public bool infinite = true;
		/// <summary>
		/// Number of times to run the speed test
		/// </summary>
		public int trials = 1;
		/// <summary>
		/// Time between tests (minutes)
		/// </summary>
		public double interval = 5;
		/// <summary>
		/// Where to put the log file
		/// </summary>
		public string logDirectory { get; set; } = ".\\";
	}
}
