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
string dataFilePath;
string logFilePath;

if (testSettings == null)
{
	Console.WriteLine("Failed to load settings.  Quitting.");
	return;
}

dataFilePath = $"{testSettings.logDirectory}/speedTestResults_{DateTime.Now.ToString("MM-dd-yyyy_HH-mm-ss")}.csv";
logFilePath = $"{testSettings.logDirectory}/log_{DateTime.Now.ToString("MM-dd-yyyy_HH-mm-ss")}.txt";

if (!Directory.Exists(Path.GetFullPath(testSettings.logDirectory)))
{
	Directory.CreateDirectory(testSettings.logDirectory);
}

Log("Finished loading settings:");
Log($"Infinite: {testSettings.infinite}");
if (!testSettings.infinite) Log($"Trials: {testSettings.trials}");
Log($"Interval: {testSettings.interval} min");
Log($"Log Dir: \"{testSettings.logDirectory}\"");
Log();

LogTestHeaders(dataFilePath);

//============ Execute Speed Test(s) ==============================================

int testsCompleted = 0;

if (testSettings.infinite)
{ 
	//never leave this loop.  User needs to kill program
	while(true)
	{
		Log($"===== Beginning Test {testsCompleted + 1} ({DateTime.Now}) ==================================================================");
		try
		{
			RunSpeedTest(true);
		}
		catch (Exception ex)
		{
			Log($"Error running speed test. Skipping this one.\n{ex.Message}");
		}

		testsCompleted++;
		Log($"Waiting {testSettings.interval} minute(s) until next test\n\n");
		Thread.Sleep((int)(testSettings.interval * 60000));
	}

}else
{
	for (int i = 0; i < testSettings.trials; i++)
	{
		Log($"===== Beginning Test {testsCompleted + 1} ({DateTime.Now}) ==================================================================");
		try
		{
			RunSpeedTest(true);
		}
		catch (Exception ex)
		{
			Log($"Error running speed test. Skipping this one.\n{ex.Message}");
		}

		testsCompleted++;
		Log($"Waiting {testSettings.interval} minute(s) until next test\n\n");
		Thread.Sleep((int)(testSettings.interval * 60000));
	}
}

Log("All tests completed");
Log($"Log file can be found at: {Path.GetFullPath(dataFilePath)}");



//============ Function definitions ===============================================

TestSettings LoadTestSettings(string filename)
{
	XmlSerializer serializer = new XmlSerializer(typeof(TestSettings));
	TestSettings i;

	if (File.Exists(filename))
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
	Log("Getting speedtest.net settings and server list...");
	client = new SpeedTestClient();
	settings = client.GetSettings();

	var servers = SelectServers();
	var bestServer = SelectBestServer(servers);
	var result = new SpeedTestResult();

	Log("Testing speed...");
	try
	{
		result.downloadSpeed = client.TestDownloadSpeed(bestServer, settings.Download.ThreadsPerUrl, 4) / 1024;
	}
	catch (Exception ex)
	{
		Log($"Error testing download speed.  Logging zero.  Error: {ex.Message}");
	}
	PrintSpeed("Download", result.downloadSpeed * 1024);

	try
	{
		result.uploadSpeed = client.TestUploadSpeed(bestServer, settings.Upload.ThreadsPerUrl, 4) / 1024;
	}
	catch (Exception ex)
	{
		Log($"Error testing upload speed.  Logging zero.  Error: {ex.Message}");
	}
	PrintSpeed("Upload", result.uploadSpeed * 1024);

	LogTestResults(result, dataFilePath);
	Log($"Test Complete");

	return result;
}

Server SelectBestServer(IEnumerable<Server> servers)
{
	Log("Best server by latency:");
	var bestServer = servers.OrderBy(x => x.Latency).First();
	PrintServerDetails(bestServer);
	Log();
	return bestServer;
}

IEnumerable<Server> SelectServers(bool verbose = false)
{
	Log();
	Log("Selecting best server by latency...");
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
	Log($"Hosted by {server.Sponsor} ({server.Name}/{server.Country}), distance: {(int)server.Distance / 1000}km, latency: {server.Latency}ms");
}

void PrintSpeed(string type, double speed)
{
	if (speed > 1024)
	{
		Log($"{type} speed: {Math.Round(speed / 1024, 2)} Mbps");
	}
	else
	{
		Log($"{type} speed: {Math.Round(speed, 2)} Kbps");
	}
}

void LogTestResults(SpeedTestResult result, string filepath)
{
	//Store blanks if there's no data (represented by -1)
	string down = result.downloadSpeed != -1 ? result.downloadSpeed.ToString() : "";
	string up = result.uploadSpeed != -1 ? result.uploadSpeed.ToString() : "";

	string[] lines = { $"{DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss tt")},{down},{up}" };
	File.AppendAllLines(filepath, lines);
}

void LogTestHeaders(string filepath)
{	
	string[] lines = { "Time,Download (Mbps),Upload (Mbps)" };
	File.AppendAllLines(filepath, lines);
}


void Log(string s = "")
{
	Console.WriteLine(s);
	string[] lines = { s };
	File.AppendAllLines(logFilePath, lines);
}