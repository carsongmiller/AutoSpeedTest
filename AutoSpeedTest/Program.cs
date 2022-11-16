// See https://aka.ms/new-console-template for more information
using AutoSpeedTest;
using System.Xml.Serialization;
using SpeedTest.Models;
using SpeedTest;


Console.WriteLine("Starting Speed Test");

//read configs from file
Console.WriteLine("Loading settings...");
TestSettings testSettings = LoadTestSettings("./settings.xml");
Settings settings;
SpeedTestClient client;
string logFilePath;

if (testSettings == null)
{
	Console.WriteLine("Failed to load settings.  Quitting.");
	return;
}
Console.WriteLine("Finished loading settings");
Console.WriteLine($"Infinite: {testSettings.infinite}");
Console.WriteLine($"Trials: {testSettings.trials}");
Console.WriteLine($"Interval: {testSettings.interval}");
Console.WriteLine($"Log Dir: {testSettings.logDirectory}");
Console.WriteLine();


logFilePath = $"{testSettings.logDirectory}/speedTestResults_{DateTime.Now.ToString("MM-dd-yyyy_HH-mm-ss")}.csv";

if (!Directory.Exists(Path.GetFullPath(testSettings.logDirectory)))
{
	Directory.CreateDirectory(testSettings.logDirectory);
}

await LogTestHeaders(logFilePath);

//============ Execute Speed Test(s) ==============================================

int testsCompleted = 0;

if (testSettings.infinite)
{ 
	//never leave this loop.  User needs to kill program
	while(true)
	{
		Console.WriteLine($"===== Beginning Test {testsCompleted + 1} ==================================================================");
		SpeedTestResult results;
		try
		{
			results = RunSpeedTest(true);
			await LogTestResults(results, logFilePath);
			Console.WriteLine($"Test Complete");
		}
		catch (Exception ex)
		{
			Console.WriteLine($"Error running speed test. Skipping this one.\n{ex.Message}");
		}
		
		testsCompleted++;
		Console.WriteLine($"Waiting {testSettings.interval} minutes until next test\n\n");
		Thread.Sleep((int)(testSettings.interval * 60000));
	}

}else
{
	for (int i = 0; i < testSettings.trials; i++)
	{
		Console.WriteLine($"===== Beginning Test {testsCompleted + 1} ==================================================================");
		var results = RunSpeedTest(true);
		testsCompleted++;
		await LogTestResults(results, logFilePath);
		Console.WriteLine("Test Complete");
		if (i + 1 >= testSettings.trials)
		{
			break;
		}
		Console.WriteLine($"Waiting {testSettings.interval} minutes until next test\n\n");
		Thread.Sleep((int)(testSettings.interval * 60000));
	}
}

Console.WriteLine("All tests completed");
Console.WriteLine($"Log file can be found at: {System.IO.Path.GetFullPath(logFilePath)}");


//============ Function definitions ===============================================


TestSettings LoadTestSettings(string filename)
{
	XmlSerializer serializer = new XmlSerializer(typeof(TestSettings));
	TestSettings i;

	if (System.IO.File.Exists(filename))
	{
		//Settings file exists.  Attempt to read
		try
		{
			using (Stream reader = new FileStream(filename, FileMode.Open))
			{
				return (TestSettings)serializer.Deserialize(reader);
			}
		}
		catch (Exception ex)
		{
			Console.WriteLine($"Error reading settings: {ex.Message}");
			return null;
		}
	}
	else
	{
		//Settings file doesn't exist, create a new one
		Console.WriteLine("No settings file exists.  Creating one with defalut settings");
		try
		{
			using (Stream writer = new FileStream(filename, FileMode.Create))
			{
				i = new TestSettings();
				serializer.Serialize(writer, i);
				return i;
			}
		}
		catch (Exception ex)
		{
			Console.WriteLine($"Error creating new settings file: {ex.Message}");
			return null;
		}
	}
}

SpeedTestResult RunSpeedTest(bool printResults = false)
{
	Console.WriteLine("Getting speedtest.net settings and server list...");
	client = new SpeedTestClient();
	settings = client.GetSettings();

	var servers = SelectServers();
	var bestServer = SelectBestServer(servers);
	var result = new SpeedTestResult();

	Console.WriteLine("Testing speed...");
	result.downloadSpeed = client.TestDownloadSpeed(bestServer, settings.Download.ThreadsPerUrl) / 1024;
	PrintSpeed("Download", result.downloadSpeed * 1024);
	result.uploadSpeed = client.TestUploadSpeed(bestServer, settings.Upload.ThreadsPerUrl) / 1024;
	PrintSpeed("Upload", result.uploadSpeed * 1024);

	return result;
}

Server SelectBestServer(IEnumerable<Server> servers)
{
	Console.WriteLine();
	Console.WriteLine("Best server by latency:");
	var bestServer = servers.OrderBy(x => x.Latency).First();
	PrintServerDetails(bestServer);
	Console.WriteLine();
	return bestServer;
}

IEnumerable<Server> SelectServers(bool verbose = false)
{
	Console.WriteLine();
	Console.WriteLine("Selecting best server by distance...");
	var servers = settings.Servers.Take(10).ToList();

	foreach (var server in servers)
	{
		server.Latency = client.TestServerLatency(server);
		if (verbose) PrintServerDetails(server);
	}
	return servers;
}

void PrintServerDetails(Server server)
{
	Console.WriteLine("Hosted by {0} ({1}/{2}), distance: {3}km, latency: {4}ms", server.Sponsor, server.Name,
		server.Country, (int)server.Distance / 1000, server.Latency);
}

void PrintSpeed(string type, double speed)
{
	if (speed > 1024)
	{
		Console.WriteLine("{0} speed: {1} Mbps", type, Math.Round(speed / 1024, 2));
	}
	else
	{
		Console.WriteLine("{0} speed: {1} Kbps", type, Math.Round(speed, 2));
	}
}

async Task LogTestResults(SpeedTestResult result, string filepath)
{
	string[] lines =
	{
		$"{DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss tt")},{result.downloadSpeed},{result.uploadSpeed}"
	};
	await File.AppendAllLinesAsync(filepath, lines);
}

async Task LogTestHeaders(string filepath)
{	
	string[] lines =
	{
		"Time,Download (Mbps),Upload (Mbps)"
	};
	await File.AppendAllLinesAsync(filepath, lines);
}