using System;
using System.Text;
using System.Diagnostics;
using System.Net;
using System.CommandLine;
using CommandLine;

namespace API_Load_Test
{

    public class APICalls
    {
        public List<int> ByteSizesList;
        public List<float> APICallsTimes;
        public int FailedCalls = 0;
        public string BearerToken;
        public int NumberOfCalls;
        public DateTime Start;
        public string GetDataURI;
        public string filepath;
        public APICalls(string BearerToken,int NumberOfCalls,DateTime Start,string GetDataURI,string filepath)
        {
            this.BearerToken = BearerToken;
            this.NumberOfCalls = NumberOfCalls;
            this.Start = Start;
            this.GetDataURI = GetDataURI;
            this.filepath = filepath;
        }

        public void MakeAPICall()
        {
            DateTime Calls_StartTime = DateTime.Now;
            int TotalNumberOfCallsCompleted = 0;
            List<int> BinningByteSizesList = new List<int>();
            List<float> BinningAPICallsTimes = new List<float>();
            Stopwatch MinuteCounter = Stopwatch.StartNew();
            int count = 0;
            int TotalByteSizes = 0;
            ByteSizesList = new List<int>();
            Program p = new Program();

            TimeSpan TotalTimes = TimeSpan.Zero;
            APICallsTimes = new List<float>();
            int FailedCallsOverTime = 0;
            for (int i = 0;i< NumberOfCalls ; i++)
            {
                Stopwatch APICallStopwatch = new Stopwatch();


                DateTime StartDate = p.GetStartDate(Start);
                HTTPRequest HTTP_GetData = new HTTPRequest(p.GetURI(StartDate,GetDataURI));

                APICallStopwatch.Start();
                HttpResponseMessage Response_GetData = HTTP_GetData.getRequestGetData(BearerToken);
                APICallStopwatch.Stop();

                HttpStatusCode code = Response_GetData.StatusCode;
                Byte[] Bytes = Response_GetData.Content.ReadAsByteArrayAsync().Result;
                int ByteSize = Bytes.Length;
                if (code == HttpStatusCode.OK)
                {
                    ByteSizesList.Add(ByteSize);
                    BinningByteSizesList.Add(ByteSize);
                }
                else
                {
                    FailedCalls = FailedCalls+1;
                    FailedCallsOverTime = FailedCallsOverTime+1;
                    ByteSizesList.Add(ByteSize);
                    BinningByteSizesList.Add(ByteSize);
                }
                APICallsTimes.Add(APICallStopwatch.ElapsedMilliseconds);
                BinningAPICallsTimes.Add(APICallStopwatch.ElapsedMilliseconds);
                TotalNumberOfCallsCompleted = TotalNumberOfCallsCompleted + 1;

                if (MinuteCounter.Elapsed.Minutes > count)
                {
                    string output = $"{DateTime.Now,-23}{Math.Round(BinningAPICallsTimes.Average(),2).ToString("0.00"),-10}{BinningAPICallsTimes.Count,-10}{Math.Round(BinningByteSizesList.Average(),2).ToString("0.00"),-10}{BinningByteSizesList.Count.ToString(),-15}{count,-15}{FailedCalls,-15}{FailedCallsOverTime,-15}{TotalNumberOfCallsCompleted,-10}";
                    Console.WriteLine(output);
                    BinningByteSizesList.Clear();
                    BinningAPICallsTimes.Clear();
                    FailedCallsOverTime = 0;
                    count = count + 1;


                }
            }
        }
    }


    public class Program
    {
        public List<int> ThreadLimit = new List<int> {};
        public List<int> CallsPerThread = new List<int> {};


        public Random gen = new Random();


        public DateTime GetStartDate(DateTime start)
        {
            int range = (DateTime.Today - start).Days;
            return start.AddDays(gen.Next(range));
        }

        public string GetURI(DateTime StartDate,string _URI)
        {
            DateTime EndDate = new DateTime();
            EndDate = StartDate.AddDays(7);
            string URI = $"{_URI}&start_dt={StartDate.ToString("yyyy/MM/dd HH:mm:ss")}&end_dt={EndDate.ToString("yyyy/MM/dd HH:mm:ss")}";

            return URI.ToString();
        }

        static void Main(string[] args)
        {
            Program p = new Program();
            List<int> NumberOfCalls =  p.CallsPerThread;
            List<int> ThreadLimit = p.ThreadLimit;
            DateTime Start = new DateTime(2022, 4, 21);
            string GetDataURI_NoDates = "https://service-dev.earthsense.co.uk/virtualzephyr/api/GetData/?vzephyr_id=180";
            string GetAuthURI = "https://service.earthsense.co.uk/authapi/api/AuthUser";
            string filepath = @"C:\Users\layre\OneDrive - Loughborough College\Industry Placement\Repos\ESLoadTester\APICallsOutput.csv";

            p.parseArguments(args);


            HTTPRequest HTTP_GetAuth = new HTTPRequest(GetAuthURI);
            HttpResponseMessage Response_GetAuth = HTTP_GetAuth.getRequest_GetAuth("LucasAyre", "UAjJIT0Mdkpm2nZb");
            string ResponseString_GetAuth = Response_GetAuth.Content.ReadAsStringAsync().Result;
            File.WriteAllText(filepath, "");
            for (int j =0; j < ThreadLimit.Count; j++)
            {
                List<APICalls> ListOfAPICalls = new List<APICalls>();


                //Phase 1 - Initialise object (list of objects)
                for (int i = 0; i < ThreadLimit[j]; i++)
                {
                    ListOfAPICalls.Add(new APICalls(ResponseString_GetAuth, NumberOfCalls[j],Start,GetDataURI_NoDates,filepath));
                }

                //Phase 2 - Assign to thread
                List<ThreadStart> ThreadStartList = new List<ThreadStart>();
                List<Thread> ThreadList = new List<Thread>();

                for (int i = 0; i < ThreadLimit[j]; i++)
                {
                    ThreadStartList.Add(ListOfAPICalls[i].MakeAPICall);
                    ThreadList.Add(new Thread(ThreadStartList[i]));
                }

                //Phase 3 - Start threads
                for (int i = 0; i < ThreadLimit[j];i ++)
                {
                    ThreadList[i].Start();
                }
                string ms = "(ms)";
                string b = "(b)";
                Console.WriteLine($"\n{"DateTime",-25}{"ms",-22}{"b",-17}{"mins/secs",-15}{"Total Fails",-15}{"Added Fails",-15}{"Total Calls"}");

                for (int i = 0; i < ThreadLimit[j]; i++)
                {
                    while (ThreadList[i].IsAlive) { }
                }

                List<float> OverallAverageCallTimes = new List<float>();
                List<double> OverallAverageSizeOfResponse = new List<double>();
                int OverallTotalNumberOfFailedCalls = new int();
                for (int i = 0; i < ThreadLimit[j]; i++)
                {
                    ThreadList[i].Join();
                    Console.WriteLine($"\nAverages for Thread {i+1}: ");
                    Console.WriteLine($"Average Length of API Call: {ListOfAPICalls[i].APICallsTimes.Average()}(ms) Average Size of Response: {ListOfAPICalls[i].ByteSizesList.Average()}(b) Number of Failed Calls:{ListOfAPICalls[i].FailedCalls}");
                    OverallAverageCallTimes.Add(ListOfAPICalls[i].APICallsTimes.Average());
                    OverallAverageSizeOfResponse.Add(ListOfAPICalls[i].ByteSizesList.Average());
                    OverallTotalNumberOfFailedCalls = OverallTotalNumberOfFailedCalls + ListOfAPICalls[i].FailedCalls;
                }
                Console.WriteLine($"\nAverages across each thread: ");
                string output = $"{OverallAverageCallTimes.Average(),5}, {Math.Round(OverallAverageSizeOfResponse.Average()),5}, {OverallTotalNumberOfFailedCalls,5}";
                Console.WriteLine(output);

                File.AppendAllText(filepath,output);
                File.AppendAllText(filepath, "\n");
            }

            
        }
        void parseArguments(string[] args)
        {
            CommandLine.Parser.Default.ParseArguments<ArgsOptions>(args).WithParsed<ArgsOptions>(o =>
            {
                if (o.UnsplitThreads != null)
                {
                    string[] StrThreadLimit = o.UnsplitThreads.Split(",");
                    for(int i = 0; i < StrThreadLimit.Length; i++)
                    {
                        ThreadLimit.Add(int.Parse(StrThreadLimit[i]));
                    }
                }

                
                if (o.UnsplitCallsPerThread != null)
                {
                    string[] strCallsPerThread = o.UnsplitCallsPerThread.Split(",");
                    for(int i = 0; i < strCallsPerThread.Length; i++)
                    {
                        CallsPerThread.Add(int.Parse(strCallsPerThread[i]));
                    }
                }
                
            });

        }
    }

    public class ArgsOptions
    {
        [CommandLine.Option("Threads", Required = true, HelpText = "Set number of threads")]
        public string UnsplitThreads { get; set; }


        [CommandLine.Option("CallsPerThread", Required = true, HelpText = "Set number of calls per thread")]
        public string UnsplitCallsPerThread { get; set; }
    }

    public class HTTPRequest
    {
        HttpClient httpClient;
        string request_string;
        HttpRequestMessage request;
        List<string> HTTPRequestStrings = new List<string>();


        public HTTPRequest(string request_string)
        {
            this.httpClient = new HttpClient();
            this.request_string = request_string;
        }


        public HttpResponseMessage getRequest_GetAuth(string Username, string Password)
        {
            string auth_string_base64;

            request = new HttpRequestMessage(HttpMethod.Get, request_string);
            auth_string_base64 = Base64Encode($"{Username}:{Password}");
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", auth_string_base64);

            return httpClient.SendAsync(request).Result;
        }
        public HttpResponseMessage getRequestGetData(string BearerToken)
        {


            request = new HttpRequestMessage(HttpMethod.Get, request_string);
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", BearerToken.ToString());

            return httpClient.SendAsync(request).Result;
        }


        public static string Base64Encode(string plainText)
        {
            byte[] plainTextBytes = System.Text.Encoding.UTF8.GetBytes(plainText);
            return System.Convert.ToBase64String(plainTextBytes);
        }
    }
}
