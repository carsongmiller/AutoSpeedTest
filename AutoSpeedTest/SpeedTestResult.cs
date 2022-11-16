using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoSpeedTest
{
	internal class SpeedTestResult
	{
		/// <summary>
		/// Mbpss
		/// </summary>
		public double downloadSpeed { get; set; }
		/// <summary>
		/// Mbps
		/// </summary>
		public double uploadSpeed { get; set; }
	}
}
