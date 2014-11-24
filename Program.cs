using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.IO;
using System.Diagnostics;

namespace simulator
{
   // Global Variables
   public static class GlobalVariable
   {
      // Configuration File Variables
      public static string version;
      public static int quantum;
      public static string scheduler;
      public static string filePath;
      public static int procTime;
      public static int monitorTime;
      public static int hdTime;
      public static int printerTime;
      public static int kbTime;
      public static string memType;
      public static string log;
      
      // Clock related variables
      public static int clockTime;

      // Thread Related Variables
      public static bool threadRunning = false;

      // Logging Related Variables
      public static bool logToFile = false;
      public static bool logToMonitor = false;
   }

   // Application Class
   class Application
   {
      public int PIDNum;
      public int numProc;
      public int procRem;
      public List<Process> procList = new List<Process>();
      public bool finished = false;

      // Sorts the application processes based on the shortest job first
      public void sortSJF()
      {
         this.procList = procList.OrderBy(o=>o.initCT).ToList();
      }
   }

   // Process Class
   class Process
   {
      public int initCT;
      public int remCT;
      public List<IO> ioList = new List<IO>();
      public bool finished = false;
   }

   // IO Class
   class IO
   {
      public int initCT;
      public int remCT;
      public int beginTime;
      public int endTime;
      public string type;
      public string descriptor;
      public bool hasStarted = false;
   }

   // Main Program
   class Program
   {
      /*-----------
        Functions
       ----------*/

      static int findCycleTime(string s)                                // Determines cycle time in our meta-data string
      {
         int len = s.Length;
         int tempInt = 0;

         for (int i = 0; i < len; i++)
         {
            if (s[i] == ')' && (i + 2) < len)
            {
               tempInt = (int)(s[i + 1] - 48) * 10;
               tempInt += (int)(s[i + 2] - 48);
            }

            else if (s[i] == ')')
            {
               tempInt = (int)(s[i + 1] - 48);
            }
         }

         return tempInt;
      }

      static string findDescriptor(string s)                            // Determines string value between ()'s in meta-data string
      {
         int len = s.Length, x = 0;
         string tempString = null;

         for (int i = 0; i < len; i++)
         {
            if (s[i] == '(')
            {
               x = i + 1;         //Set the tempInt to the letter after (

               while (s[x] != ')')  //Iterate through and get characters between ()
               {
                  tempString += s[x];
                  x++;
               }
            }
         }

         return tempString;//Return the value (i.e. "monitor" or "keyboard")
      }

      static void copyProcess(Process src, Process dest)                // Makes a copy of a process
      {
         dest.initCT = src.initCT;
         dest.remCT = src.remCT;

         for (int x = 0; x < src.ioList.Count; x++)
         {
            IO newIO = new IO();
            newIO.initCT = src.ioList[x].initCT;
            newIO.remCT = src.ioList[x].remCT;
            newIO.type = src.ioList[x].type;
            newIO.descriptor = src.ioList[x].descriptor;
            dest.ioList.Add(newIO);
         }
      }

      static void copyApplication(Application src, Application dest)    // Makes a copy of an application
      {
         dest.PIDNum = src.PIDNum;
         dest.numProc = src.numProc;
         dest.procRem = src.procRem;

         for (int x = 0; x < src.procList.Count; x++)
         {
            Process newProcess = new Process();
            copyProcess(src.procList[x], newProcess);  //Store source process in tmp
            dest.procList.Add(newProcess);
         }
      }

      static int findIndexOfNextProc(Application currApp)               // Finds the index of the next process in an application
      {
         for (int x = 0; x < currApp.procList.Count; x++)
         {
            if (currApp.procList[x].finished == false)
            {
               return x;
            }
         }

         return -1;
      }

      static int findIndexOfNextIO(Process currProc)                    // Finds the index of the next IO in a process
      {
         for (int x = 0; x < currProc.ioList.Count; x++)
         {
            if (currProc.ioList[x].remCT != 0)
            {
               if (currProc.ioList[x].hasStarted == false)
               {
                  return x;
               }
            }
         }

         return -1;
      }

      static long calcMicroSec( long ticks )                            // Converts to from ticks to microseconds
      {
         long microSec;

         microSec = (ticks * 1000000) / Stopwatch.Frequency;

         return microSec;
      }

      static void startThread(Application threadedApp, IO threadedIO)   // The function a thread runs when IO operation has started
      {
         int procTime = 0;

         switch (threadedIO.descriptor)
         {
            case "monitor":
               procTime = threadedIO.initCT * GlobalVariable.monitorTime;
               break;

            case "hard drive":
               procTime = threadedIO.initCT * GlobalVariable.hdTime;
               break;

            case "printer":
               procTime = threadedIO.initCT * GlobalVariable.printerTime;
               break;

            case "keyboard":
               procTime = threadedIO.initCT * GlobalVariable.kbTime;
               break;        
         }

         GlobalVariable.threadRunning = true;

         while (GlobalVariable.clockTime < threadedIO.endTime)
         {
            //Wait for clock to be past endtime
         }

         //The IO Operation has finished, so set remCT to 0
         threadedIO.remCT = 0;
         //Display message that notifies it finished

         log("PID " + threadedApp.PIDNum + " – " + threadedIO.type + ", " + threadedIO.descriptor + " completed (" + procTime + " mSec)");
         GlobalVariable.threadRunning = false;
      }

      static void log(string str)                                       // Log to monitor, log to file, or both
      {
         if (GlobalVariable.logToMonitor == true)
         {
            Console.WriteLine(str);
         }

         if (GlobalVariable.logToFile == true)
         {
            logData.Add(str);
         }
      }

      public static List<string> logData = new List<string>();          // Global list that stores our log data

      static void Main(string[] args)
      {
         /*-----------
           Variables 
         -----------*/

         string configFile;          // Variable to store the configuration file
         bool appStarted = false,    // States if an application is started
              procStarted = false;   // States if a process is started
         int appIndex = 0;           // Application index
         long microSec;              // Variable to store calculated microseconds

         List<string> ourData = new List<string>();               // Temporary list to hold all meta data
         List<Application> ourAppList = new List<Application>();  // Our application list
         Queue<int> appQueue = new Queue<int>();                  // Queue to determine which application to run next

         Stopwatch realTime = new Stopwatch();                    // Creates a timer for to simulate real-time, used for system output
         Stopwatch startTime = new Stopwatch();                   // Creates a timer to measure the start-time

         Application tempApp = new Application();  // Temporary application for storing purposes
         Process tempProc = new Process();         // Temporary process for storing purposes
         int tempInt = 0;                          // Temporary integer

         /*-----------------------------------------------------------
           Read in the configuration file from command-line argument 
         -----------------------------------------------------------*/

         startTime.Start();

         if (args.Length == 0)      // Checks if user had input an argument, if not, exit the program
         {
            Console.WriteLine("Error: Please enter the name of the configuration file. \nPress a key to exit.");
            Console.ReadKey();
            Environment.Exit(1);
         }

         else    // If there is a valid argument, start reading in data
         {
            configFile = args[0];                           // Get the configuration file name

            try      // Read in all the lines to an array, prints an error if an exception is caught
            {
               string[] lines = File.ReadAllLines(configFile);
               int index;

               for (index = 0; index < lines.Length; index++)
               {
                  if( index == 4 )     // If it's file path, only get the string characters past the first colon
                  {
                     lines[index] = lines[index].Substring(11);
                     index++;
                  }

                  if (lines[index].Contains(":"))
                  {
                     lines[index] = lines[index].Split(':')[1];   // Get the right side of the colon and replace in previous array
                     lines[index] = lines[index].Trim();          // Gets rid of white-space
                  }
               }

               // Update Global Variables
               GlobalVariable.version = lines[1];
               GlobalVariable.quantum = int.Parse(lines[2]);
               GlobalVariable.scheduler = lines[3];
               GlobalVariable.filePath = lines[4];
               GlobalVariable.procTime = int.Parse(lines[5]);
               GlobalVariable.monitorTime = int.Parse(lines[6]);
               GlobalVariable.hdTime = int.Parse(lines[7]);
               GlobalVariable.printerTime = int.Parse(lines[8]);
               GlobalVariable.kbTime = int.Parse(lines[9]);
               GlobalVariable.memType = lines[10];
               GlobalVariable.log = lines[11];

               // Determine if configuration file had a valid scheduling algorithm, else print an error
               if( GlobalVariable.scheduler != "FIFO" && GlobalVariable.scheduler != "RR" && GlobalVariable.scheduler != "SJF" )
               {
                  Console.WriteLine("Error: The scheduling type in the configuration file is not supported. \nTry again using FIFO, RR, or SJF. \nPress a key to exit.");
                  Console.ReadKey();
                  Environment.Exit(1);
               }
            }

            catch    // Print out an error if the configuration file could not be read
            {
               Console.WriteLine("Error: Problems reading the configuration file. \nPress a key to exit.");
               Console.ReadKey();
               Environment.Exit(1);
            }
         }

         /*----------------------------------------------------
           Read in the meta-data to their respectable classes
         ----------------------------------------------------*/

         // If the config file states that the OS needs to log to both a file and the monitor we set the bools to true 
         // so that our logging function knows to send the output to a file and the monitor
         if (GlobalVariable.log == "Log to Both")
         {
            GlobalVariable.logToFile = true;
            GlobalVariable.logToMonitor = true;
         }

         // If the config file states that the OS needs to only log to a file, we set a bool to true 
         // so that our logging function knows to send the output to a file
         else if (GlobalVariable.log == "Log to File")
         {
            GlobalVariable.logToFile = true;
         }

         // If the config file states that the OS needs to only log to the monitor, we set a bool to true 
         // so that our logging function knows to send the output to the monitor
         else
         {
            GlobalVariable.logToMonitor = true;
         }

         if (File.Exists(Directory.GetCurrentDirectory() + "/log.txt"))
         {
            File.Delete(Directory.GetCurrentDirectory() + "/log.txt");
         }

         // Here we open the file containing the meta data for our OS to process
         FileStream readStream = File.OpenRead(GlobalVariable.filePath + "/metadata.txt");
         TextReader textReader = new StreamReader(readStream);

         // This is so that we can ignore the first line of the metadata file as well as the blank line
         string dummy = textReader.ReadLine();
         dummy = textReader.ReadLine();

         // The textReader will get the rest of the text from the metadata file and place it in a string where it can be further processed
         string whatWasReadIn = textReader.ReadToEnd();

         textReader.Close();// close the metadata file since we no longer need it

         // index1 is going to start as the first spot in the string and index 2 is going to start at the location of the first ;
         // this is so that we can split the string into separate strings containing only the information between ;
         int index1 = 0, index2 = whatWasReadIn.IndexOf(';');

         // while the second index is within the bounds of our data (its checking index2 since that one is always going to be farther than index1)
         while (index2 != -1)
         {
            // We have a variable to hold the difference between index1 and index2 so that when we call the Substring function, we can give it a specific length to read
            // temp is going to be given a substring of whatWasReadIn starting at index1 and going for a length equal to the difference
            int difference = index2 - index1;
            string temp = whatWasReadIn.Substring(index1, difference);

            // temp will trim the white space from the front and back of the temp string and add it to a list of data
            temp = temp.Trim();
            ourData.Add(temp);

            // index 1 is going to be set where index2 was and index 2 will be set to the ; after the one it is currently on
            index1 = index2 + 1;
            index2 = whatWasReadIn.IndexOf(';', index2 + 1);
         }

         // This code is only to make sure that the last part of the meta data which will be s(end) is taken and placed into our data as well
         string temp2 = whatWasReadIn.Substring(index1);
         temp2 = temp2.Trim();
         ourData.Add(temp2);

         //Begin to store data in their proper structures (ourAppList)
         foreach (string currentLine in ourData)
         {
            switch (currentLine[0])    // Switch statement to compare the first character of each string from meta-data
            {
               // Operating System Operations, Start & End
               case 'S':
                  {
                     if (findDescriptor(currentLine) == "start")     // If it's the start of the system, display output
                     {
                        startTime.Stop();
                        microSec = calcMicroSec(startTime.ElapsedTicks);
                        log("SYSTEM - Boot, set up (" + startTime.ElapsedMilliseconds + " mSec | " + microSec + " \u00b5Sec)");
                     }

                     else     // If this is the end of the meta-data, make sure everything has been added in
                     {
                        if (procStarted)
                        {
                           Process tempProc2 = new Process();
                           copyProcess(tempProc, tempProc2);
                           tempApp.procList.Add(tempProc2);
                           procStarted = false;
                        }

                        if (appStarted)
                        {
                           Application tempApp2 = new Application();
                           tempInt = tempApp.procList.Count;
                           tempApp.numProc = tempInt;
                           tempApp.procRem = tempInt;
                           copyApplication(tempApp, tempApp2);
                           ourAppList.Add(tempApp2);
                           appStarted = false;
                        }
                     }
                     break;
                  }

               // Program Application Operations, Start & End
               case 'A':
                  {
                     if (findDescriptor(currentLine) == "start")     // Begin the process if creating an application
                     {
                        realTime.Start();
                        appStarted = true;
                        tempApp.procList.Clear();
                        appQueue.Enqueue(appIndex);                        // Adds an index to our appQueue
                        appIndex++;                                        // Increment number of applications
                        tempApp.PIDNum = appIndex;
                        log("PID " + appIndex + " - Enter system");
                     }

                     else     // If this is the end of an application, store values into list                                                
                     {
                        Application tempApp2 = new Application();
                        Process tempProc2 = new Process();
                        if (procStarted)
                        {
                           copyProcess(tempProc, tempProc2);
                           tempApp.procList.Add(tempProc2);
                           procStarted = false;
                        }
                        tempInt = tempApp.procList.Count;
                        tempApp.numProc = tempInt;
                        tempApp.procRem = tempInt;
                        copyApplication(tempApp, tempApp2);
                        ourAppList.Add(tempApp2);

                        realTime.Stop();
                        microSec = calcMicroSec(realTime.ElapsedTicks);
                        log("SYSTEM - Creating PID " + appIndex + " (" + realTime.ElapsedMilliseconds + " mSec | " + microSec + " \u00b5Sec)");
                        realTime.Reset();

                        appStarted = false;
                     }
                     break;
                  }

               // Process Operations, Run
               case 'P':
                  {
                     // Copy
                     if (procStarted)     // If a process is being created, copy that process and begin a new one
                     {
                        Process tempProc2 = new Process();
                        copyProcess(tempProc, tempProc2);
                        tempApp.procList.Add(tempProc2);
                        procStarted = false;
                     }

                     if (procStarted == false)     // Begin creating a process
                     {
                        tempInt = findCycleTime(currentLine);
                        tempProc.initCT = tempInt;
                        tempProc.remCT = tempInt;
                        tempProc.ioList.Clear();
                        procStarted = true;
                     }
                     break;
                  }

               // Input Operations, Hard Drive & Keyboard
               case 'I':
                  {
                     IO tempIO = new IO();
                     tempInt = findCycleTime(currentLine);
                     tempIO.initCT = tempInt;
                     tempIO.remCT = tempInt;
                     tempIO.type = "Input";
                     tempIO.descriptor = findDescriptor(currentLine);
                     if (procStarted == false)     // If there is no process to attach the IO to, create one
                     {
                        Process tempProc2 = new Process();
                        tempProc.initCT = 0;
                        tempProc.remCT = 0;
                        tempProc.ioList.Add(tempIO);
                        copyProcess(tempProc, tempProc2);
                        tempApp.procList.Add(tempProc2);
                     }

                     else 
                     {
                        tempProc.ioList.Add(tempIO);        // Add to the last current running process
                     }
                     break;
                  }

               // Output Operations, Hard Drive & Monitor
               case 'O':
                  {
                     IO tempIO = new IO();
                     tempInt = findCycleTime(currentLine);
                     tempIO.initCT = tempInt;
                     tempIO.remCT = tempInt;
                     tempIO.type = "Output";
                     tempIO.descriptor = findDescriptor(currentLine);
                     if (procStarted == false)     // If there is no process to attach the IO to, create one
                     {
                        Process tempProc2 = new Process();
                        tempProc.initCT = 0;
                        tempProc.remCT = 0;
                        tempProc.ioList.Add(tempIO);
                        copyProcess(tempProc, tempProc2);
                        tempApp.procList.Add(tempProc2);
                     }

                     else
                     {
                        tempProc.ioList.Add(tempIO);        //Add to the last current running process
                     }              
                     break;
                  }

               default:
                  { 
                     Console.WriteLine("Error: Problems occured reading the meta-data. \nPress a key to exit.");
                     Console.ReadKey();
                     Environment.Exit(1);
                     break;
                  }
            }
         }

         /*-------------------------------------------------------------------------
           Begin the main loop of the simulation based on the scheduling algorithm
         -------------------------------------------------------------------------*/

         realTime.Reset();

         if (GlobalVariable.scheduler == "FIFO" || GlobalVariable.scheduler == "SJF")
         {
            if (GlobalVariable.scheduler == "SJF")    // Sort shortest job first before processing
            {
               // Sort all processes 
               for (int w = 0; w < ourAppList.Count; w++)
               {
                  ourAppList[w].sortSJF();
               }
            }

            // Run the program in first-in-first-out order
            while (appQueue.Count != 0)        // While the queue is NOT empty we still need to continue processing
            {
               Thread.Sleep(1);                                                     // Test for interupt (1millisecond)
               int currAppIndex = appQueue.Dequeue();                               // Get an application from the queue to begin processing
               int currProcIndex = findIndexOfNextProc(ourAppList[currAppIndex]);   // Returns -1 if complete
               int currIOIndex = -1;

               realTime.Start();

               if (ourAppList[currAppIndex].procList[currProcIndex].remCT == 0)     // Begin IO if processing is finished
               {
                  //Handle IO Here
                  Thread.Sleep(1);    //test for interupt (1millisecond)
                  currIOIndex = findIndexOfNextIO(ourAppList[currAppIndex].procList[currProcIndex]);  // Find next IO to handle

                  ourAppList[currAppIndex].procList[currProcIndex].ioList[currIOIndex].hasStarted = true;
                  ourAppList[currAppIndex].procList[currProcIndex].ioList[currIOIndex].beginTime = GlobalVariable.clockTime;
                  ourAppList[currAppIndex].procList[currProcIndex].ioList[currIOIndex].endTime = (GlobalVariable.clockTime + ourAppList[currAppIndex].procList[currProcIndex].ioList[currIOIndex].initCT);
                  Thread.Sleep(1);    //test for interupt (1millisecond)
                  realTime.Stop();
                  microSec = calcMicroSec(realTime.ElapsedTicks);
                  log("SYSTEM – Managing I/O  (" + realTime.ElapsedMilliseconds + " mSec | " + microSec + " \u00b5Sec)");
                  log("PID " + (currAppIndex + 1) + " - " + ourAppList[currAppIndex].procList[currProcIndex].ioList[currIOIndex].type + ", " + ourAppList[currAppIndex].procList[currProcIndex].ioList[currIOIndex].descriptor + " started");
                  realTime.Reset();

                  Thread.Sleep(1000);    // Test for interrupt (1second)
                  Thread thread = new Thread(() => startThread(ourAppList[currAppIndex], ourAppList[currAppIndex].procList[currProcIndex].ioList[currIOIndex])); ;
                  thread.Start();
                  Thread.Sleep(1000);    // Test for interrupt (1second)
                  currIOIndex = findIndexOfNextIO(ourAppList[currAppIndex].procList[currProcIndex]);


                  if (currIOIndex == -1)     // Checks if IO is finished in the current process
                  {
                     ourAppList[currAppIndex].procList[currProcIndex].finished = true;
                  }
               }
               else
               {
                  while (ourAppList[currAppIndex].procList[currProcIndex].remCT != 0)     // Begin processing for an application
                  {
                     // Decrease remCT and increment the clock variable
                     Thread.Sleep(1);    // Test for interupt (1millisecond)
                     ourAppList[currAppIndex].procList[currProcIndex].remCT--;
                     GlobalVariable.clockTime++;
                  }

                  int procTime = ourAppList[currAppIndex].procList[currProcIndex].initCT * GlobalVariable.procTime;

                  log("PID " + (currAppIndex + 1) + " - Processing (" + procTime + " mSec)");

                  if (ourAppList[currAppIndex].procList[currProcIndex].ioList.Count == 0)
                  {
                     ourAppList[currAppIndex].procList[currProcIndex].finished = true; //If the process has no corresponding IO its finished
                  }
               }
               //Once a process is finished check if entire app is finished
               currProcIndex = findIndexOfNextProc(ourAppList[currAppIndex]);
               if (currProcIndex != -1)
               {
                  //If app is not finished enqueue it back to our appQueue
                  appQueue.Enqueue(currAppIndex);
               }//If currProcIndex does = -1, the app is finished

               else
               {
                  while (GlobalVariable.threadRunning && appQueue.Count == 0)
                  {
                     GlobalVariable.clockTime++;
                     Thread.Sleep(1);
                  }
                  log("PID " + (currAppIndex + 1) + " – Exit system");
                  realTime.Stop();
                  microSec = calcMicroSec(realTime.ElapsedTicks);
                  log("SYSTEM – Ending process (" + realTime.ElapsedMilliseconds + " mSec | " + microSec + " \u00b5Sec)");
                  realTime.Reset();
               }

               if (appQueue.Count > 0)
               {
                  realTime.Stop();
                  microSec = calcMicroSec(realTime.ElapsedTicks);
                  log("SYSTEM – Swapping processes (" + realTime.ElapsedMilliseconds + " mSec | " + microSec + " \u00b5Sec)");
                  realTime.Reset();
               }
            }

         }

         else if (GlobalVariable.scheduler == "RR")
         {
            // Run the program in first-in-first-out order
            while (appQueue.Count != 0)        // While the queue is NOT empty we still need to continue processing 
            {
               Thread.Sleep(1);    //test for interupt (1millisecond)
               int currAppIndex = appQueue.Dequeue();
               int currProcIndex = findIndexOfNextProc(ourAppList[currAppIndex]);//Returns -1 if complete
               int currIOIndex = -1;

               realTime.Start();

               if (ourAppList[currAppIndex].procList[currProcIndex].remCT == 0)
               {
                  //Handle IO Here
                  Thread.Sleep(1);    //test for interupt (1millisecond)
                  currIOIndex = findIndexOfNextIO(ourAppList[currAppIndex].procList[currProcIndex]);  //Find next IO to handle
                  ourAppList[currAppIndex].procList[currProcIndex].ioList[currIOIndex].hasStarted = true;
                  ourAppList[currAppIndex].procList[currProcIndex].ioList[currIOIndex].beginTime = GlobalVariable.clockTime;
                  ourAppList[currAppIndex].procList[currProcIndex].ioList[currIOIndex].endTime = (GlobalVariable.clockTime + ourAppList[currAppIndex].procList[currProcIndex].ioList[currIOIndex].initCT);
                  Thread.Sleep(1);    //test for interupt (1millisecond)

                  realTime.Stop();
                  microSec = calcMicroSec(realTime.ElapsedTicks);
                  log("SYSTEM – Managing I/O  (" + realTime.ElapsedMilliseconds + " mSec | " + microSec + " \u00b5Sec)");
                  log("PID " + (currAppIndex + 1) + " - " + ourAppList[currAppIndex].procList[currProcIndex].ioList[currIOIndex].type + ", " + ourAppList[currAppIndex].procList[currProcIndex].ioList[currIOIndex].descriptor + " started");
                  realTime.Reset();

                  Thread.Sleep(1000);    // Test for interupt (1second)
                  Thread thread = new Thread(() => startThread(ourAppList[currAppIndex], ourAppList[currAppIndex].procList[currProcIndex].ioList[currIOIndex])); ;
                  thread.Start();
                  Thread.Sleep(1000);    // Test for interupt (1second)
                  currIOIndex = findIndexOfNextIO(ourAppList[currAppIndex].procList[currProcIndex]);

                  if (currIOIndex == -1)
                  {
                     ourAppList[currAppIndex].procList[currProcIndex].finished = true;
                  }
               }
               else
               {
                  int cyclesRan = 0;

                  for (int q = 0; q < GlobalVariable.quantum; q++)
                  {

                     if (ourAppList[currAppIndex].procList[currProcIndex].remCT > 0)
                     {
                        //Decrease remCT and increment the clock variable
                        Thread.Sleep(1);    //test for interupt (1millisecond)
                        ourAppList[currAppIndex].procList[currProcIndex].remCT--;
                        GlobalVariable.clockTime++;
                        cyclesRan++;
                     }
                  }

                  int procTime = cyclesRan * GlobalVariable.procTime;

                  log("PID " + (currAppIndex + 1) + " - Processing (" + procTime + " mSec)");

                  if (ourAppList[currAppIndex].procList[currProcIndex].remCT == 0)//if proc rem ct = 0
                  {
                     if (ourAppList[currAppIndex].procList[currProcIndex].ioList.Count == 0)//test if no io (to set finished)
                     {
                        ourAppList[currAppIndex].procList[currProcIndex].finished = true; //If the process has no corresponding IO its finished
                     }
                  }
               }
               //Once a process is finished check if entire app is finished
               currProcIndex = findIndexOfNextProc(ourAppList[currAppIndex]);

               if (currProcIndex != -1)
               {
                  //If app is not finished enqueue it back to our appQueue
                  appQueue.Enqueue(currAppIndex);
               }//If currProcIndex does = -1, the app is finished

               else
               {
                  while (GlobalVariable.threadRunning && appQueue.Count == 0)
                  {
                     GlobalVariable.clockTime++;
                     Thread.Sleep(1);
                  }

                  log("PID " + (currAppIndex + 1) + " – Exit system");
                  realTime.Stop();
                  microSec = calcMicroSec(realTime.ElapsedTicks);
                  log("SYSTEM – Ending process (" + realTime.ElapsedMilliseconds + " mSec | " + microSec + " \u00b5Sec)");
                  realTime.Reset();
               }

               if (appQueue.Count > 0)
               {
                  realTime.Stop();
                  microSec = calcMicroSec(realTime.ElapsedTicks);
                  log("SYSTEM – Swapping processes (" + realTime.ElapsedMilliseconds + " mSec | " + microSec + " \u00b5Sec)");
                  realTime.Reset();
               }
            }
         }

         if ( GlobalVariable.logToFile )
         {
            using (StreamWriter writer = new StreamWriter(Directory.GetCurrentDirectory() + "//log.txt", true))
            {
               foreach (String line in logData)
               {
                  Console.WriteLine(line);
                  writer.WriteLine(line);
               }
            }
         }

         Console.ReadKey();
         Environment.Exit(0);

      }
   }
}
