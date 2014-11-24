﻿using System;
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
         this.procList = procList.OrderBy(o => o.initCT).ToList();
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

      static long calcMicroSec(long ticks)                            // Converts to from ticks to microseconds
      {
         long microSec;

         microSec = (ticks * 1000000) / Stopwatch.Frequency;

         return microSec;
      }

      static void startThread(Application threadedApp, IO threadedIO)   // The function a thread runs when IO operation has started
      {
         GlobalVariable.threadRunning = true;

         while (GlobalVariable.clockTime < threadedIO.endTime)
         {
            //Wait for clock to be past endtime
         }

         //The IO Operation has finished, so set remCT to 0
         threadedIO.remCT = 0;
         //Display message that notifies it finished
         log("PID " + threadedApp.PIDNum + " – " + threadedIO.type + ", " + threadedIO.descriptor + " completed (" + (GlobalVariable.kbTime * threadedIO.initCT) + "msec)");
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
                  if (index == 4)     // If it's file path, only get the string characters past the first colon
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
               if (GlobalVariable.scheduler != "FIFO" && GlobalVariable.scheduler != "RR" && GlobalVariable.scheduler != "SJF")
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

         // This is for the config wanting us to log to the monitor as well as a file
         if (GlobalVariable.log == "Log to Both")
         {
            GlobalVariable.logToFile = true;
            GlobalVariable.logToMonitor = true;
         }

         // This is for the config only wanting us to log to a file
         else if (GlobalVariable.log == "Log to File")
         {
            GlobalVariable.logToFile = true;
         }

         // This is for the config only wanting us to log to the monitor
         else
         {
            GlobalVariable.logToMonitor = true;
         }

         // This is to make sure that if the log file exists from a previous run of the system it will be overwritten.
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

         // the textReader will get the rest of the text from the metadata file and place it in a string where it can be further processed
         string whatWasReadIn = textReader.ReadToEnd();

         textReader.Close();// close the metadata file since we no longer need it

         // index1 is going to start as the first spot in the string and index 2 is going to start at the location of the first ;
         // this is so that we can split the string into separate strings containing only the information between ;
         int index1 = 0, index2 = whatWasReadIn.IndexOf(';');

         // while the second index is within the bounds of our data (its checking index2 since that one is always going to be farther than index1)
         while (index2 != -1)
         {
            // we have a variable to hold the difference between index1 and index2 so that when we call the Substring function, we can give it a specific length to read
            // temp is going to be given a substring of whatWasReadIn starting at index1 and going for a length equal to the difference
            int difference = index2 - index1;
            string temp = whatWasReadIn.Substring(index1, difference);

            //temp will trim the white space from the front and back of the temp string and add it to a list of data
            temp = temp.Trim();
            ourData.Add(temp);

            // index 1 is going to be set where index2 was and index 2 will be set to the ; after the one it is currently on
            index1 = index2 + 1;
            index2 = whatWasReadIn.IndexOf(';', index2 + 1);
         }

         // this code is only to make sure that the last part of the meta data which will be s(end) is taken and placed into our data as well
         string temp2 = whatWasReadIn.Substring(index1);
         temp2 = temp2.Trim();
         ourData.Add(temp2);

         //Begin to store data in their proper structures (ourAppList)
         foreach (string currentLine in ourData)
         {
            switch (currentLine[0])
            {
               // Operating System Operations, Start & End
               case 'S':
                  {
                     // this is only to be done at the very start of the data for S(start)
                     if (findDescriptor(currentLine) == "start")
                     {
                        // this is to state how long it took to boot up the system
                        startTime.Stop();
                        microSec = calcMicroSec(startTime.ElapsedTicks);
                        log("SYSTEM - Boot, set up (" + startTime.ElapsedMilliseconds + " mSec | " + microSec + " \u00b5Sec)");
                     }

                     // If this is the end of the meta-data, make sure everything has been added in
                     else
                     {
                        // a process has been started but not finished, finish loading it to the structure
                        if (procStarted)
                        {
                           Process tempProc2 = new Process();
                           copyProcess(tempProc, tempProc2);
                           tempApp.procList.Add(tempProc2);
                           procStarted = false;
                        }

                        // an application has been started but not finished, finish loading it loading it to the structure
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
                     // when he hit the starting statement of an application, we start the clock, enqueue and increment the index of the application, then set that index as the PID of the Application
                     if (findDescriptor(currentLine) == "start")
                     {
                        realTime.Start();
                        appStarted = true;                                 // This is to show that we are currently loading an application into the structure
                        tempApp.procList.Clear();
                        appQueue.Enqueue(appIndex);                        //Adds an index to our appQueue
                        appIndex++;                                        // Increment number of applications
                        tempApp.PIDNum = appIndex;
                        log("PID " + appIndex + " - Enter system");
                     }

                     // If this is the end of an application, store values into list
                     else
                     {
                        // declare temps to transfer information
                        Application tempApp2 = new Application();
                        Process tempProc2 = new Process();

                        // if a process has been started for this application, add it to this applications list of processes
                        if (procStarted)
                        {
                           copyProcess(tempProc, tempProc2);
                           tempApp.procList.Add(tempProc2);
                           procStarted = false;
                        }

                        // count the number of processes for this application and how many still need to be completed.
                        tempInt = tempApp.procList.Count;
                        tempApp.numProc = tempInt;
                        tempApp.procRem = tempInt;

                        // copy the application to a temp so that it can be added to our list of applications
                        copyApplication(tempApp, tempApp2);
                        ourAppList.Add(tempApp2);

                        // stop the clock and output how long it took to create the Application
                        realTime.Stop();
                        microSec = calcMicroSec(realTime.ElapsedTicks);
                        log("SYSTEM - Creating PID " + appIndex + " (" + realTime.ElapsedMilliseconds + " mSec | " + microSec + " \u00b5Sec)");
                        realTime.Reset();

                        // reset this to false to show that we finished the current application we were working on loading into the structure
                        appStarted = false;
                     }
                     break;
                  }

               // Process Operations, Run
               case 'P':
                  {
                     // if we have already started loading a process into our structure
                     // Copy
                     if (procStarted)
                     {
                        Process tempProc2 = new Process();
                        copyProcess(tempProc, tempProc2);

                        // add it to our list of processes for the current application and reset the bool to show we are done loading it.
                        tempApp.procList.Add(tempProc2);
                        procStarted = false;
                     }

                     // if we have not started loading a process, begin looking for information on the current process.
                     if (procStarted == false)
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
                     // create a temp IO variable to load info into
                     IO tempIO = new IO();

                     // find the cycle time and place that information into the initCT and remCT for the temp
                     tempInt = findCycleTime(currentLine);
                     tempIO.initCT = tempInt;
                     tempIO.remCT = tempInt;

                     // give it the type Input and determine what the descriptor is for this Input
                     tempIO.type = "Input";
                     tempIO.descriptor = findDescriptor(currentLine);


                     if (procStarted == false)
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

               // Output Operations, Hard Drive & Monitor
               case 'O':
                  {
                     // create a temp IO variable to load info into
                     IO tempIO = new IO();

                     // find the cycle time and place that information into the initCT and remCT for the temp
                     tempInt = findCycleTime(currentLine);
                     tempIO.initCT = tempInt;
                     tempIO.remCT = tempInt;

                     // give it the type Output and determine what the descriptor is for this Output
                     tempIO.type = "Output";
                     tempIO.descriptor = findDescriptor(currentLine);

                     if (procStarted == false)
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
                     // if there is something wrong in the metadata file, output an error message and exit the program
                     Console.WriteLine("Error: Problems occured reading the meta-data. \nPress a key to exit.");
                     Console.ReadKey();
                     Environment.Exit(1);
                     break;
                  }
            }
         }

         // This is for the scheduler being either FIFO or SJF since the coding is the same except for the processes being sorted from least to greatest if SJF is chosen
         if (GlobalVariable.scheduler == "FIFO" || GlobalVariable.scheduler == "SJF")
         {
            // this is ony for the scheduler being declared as SJF which means the processes get sorted before being processed
            if (GlobalVariable.scheduler == "SJF")
            {
               //Sort all processes 
               for (int w = 0; w < ourAppList.Count; w++)
               {
                  ourAppList[w].sortSJF();
               }
            }

            //Run the program in first in first out order while there is still information in the queue to be processed
            while (appQueue.Count != 0)        //While the queue is NOT empty we still need 
            {
               Thread.Sleep(1);    //test for interupt (1millisecond)
               int currAppIndex = appQueue.Dequeue(); // dequeue the first index from the queue of applications
               int currProcIndex = findIndexOfNextProc(ourAppList[currAppIndex]); // Find the index of the next process to run within this application, Returns -1 if all processes for this application are complete
               int currIOIndex = -1;

               // if the current process of the current application is complete, the IO for that process gets handled
               if (ourAppList[currAppIndex].procList[currProcIndex].remCT == 0)
               {
                  // start the clock
                  realTime.Start();
                  //Handle IO Here
                  Thread.Sleep(1);    //test for interupt (1millisecond)
                  currIOIndex = findIndexOfNextIO(ourAppList[currAppIndex].procList[currProcIndex]);  //Find next IO to handle
                  ourAppList[currAppIndex].procList[currProcIndex].ioList[currIOIndex].hasStarted = true; // set the bool to true so that the OS knows the IO has started
                  ourAppList[currAppIndex].procList[currProcIndex].ioList[currIOIndex].beginTime = GlobalVariable.clockTime;
                  ourAppList[currAppIndex].procList[currProcIndex].ioList[currIOIndex].endTime = (GlobalVariable.clockTime + ourAppList[currAppIndex].procList[currProcIndex].ioList[currIOIndex].initCT);
                  Thread.Sleep(1);    //test for interupt (1millisecond)
                  realTime.Stop();

                  // let the user know the IO is started and that the system is swapping processes
                  microSec = calcMicroSec(realTime.ElapsedTicks);
                  log("SYSTEM – Managing I/O  (" + realTime.ElapsedMilliseconds + " mSec | " + microSec + " \u00b5Sec)");
                  log("PID " + (currAppIndex + 1) + " - " + ourAppList[currAppIndex].procList[currProcIndex].ioList[currIOIndex].type + ", " + ourAppList[currAppIndex].procList[currProcIndex].ioList[currIOIndex].descriptor + " started");
                  realTime.Reset();

                  // this is to check if the IO we were just working on has completed
                  Thread.Sleep(1000);    //test for interupt (1millisecond)
                  Thread thread = new Thread(() => startThread(ourAppList[currAppIndex], ourAppList[currAppIndex].procList[currProcIndex].ioList[currIOIndex])); ;
                  thread.Start();
                  Thread.Sleep(1000);    //test for interupt (1second)
                  currIOIndex = findIndexOfNextIO(ourAppList[currAppIndex].procList[currProcIndex]);

                  // if it has, set the IO's bool to true so we know its done
                  if (currIOIndex == -1)
                  {
                     ourAppList[currAppIndex].procList[currProcIndex].finished = true;
                  }
               }

               // if the current process hasn't finished yet finish it before we worry about it's IO
               else
               {
                  while (ourAppList[currAppIndex].procList[currProcIndex].remCT != 0)
                  {
                     //Decrease remCT and increment the clock variable
                     Thread.Sleep(1);    //test for interupt (1millisecond)
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

               // if the current application is finished then don't enqueue it
               if (currProcIndex != -1)
               {
                  //If app is not finished enqueue it back to our appQueue
                  appQueue.Enqueue(currAppIndex);
               }//If currProcIndex does = -1, the app is finished

               // this is to make sure the process doesn't quit before the thread with its IO has finished
               else
               {
                  while (GlobalVariable.threadRunning && appQueue.Count == 0)
                  {
                     GlobalVariable.clockTime++;
                     Thread.Sleep(1);
                  }
                  log("PID " + (currAppIndex + 1) + " – Exit system");
                  realTime.Start();
                  realTime.Stop();
                  microSec = calcMicroSec(realTime.ElapsedTicks);
                  log("SYSTEM – Ending process (" + realTime.ElapsedMilliseconds + " mSec | " + microSec + " \u00b5Sec)");
                  realTime.Reset();
               }

               // if there is still information in the queue of applications let the user know the os is swapping processes
               if (appQueue.Count > 0)
               {
                  realTime.Start();
                  realTime.Stop();
                  microSec = calcMicroSec(realTime.ElapsedTicks);
                  log("SYSTEM – Swapping processes (" + realTime.ElapsedMilliseconds + " mSec | " + microSec + " \u00b5Sec)");
                  realTime.Reset();
               }
            }

         }

         else if (GlobalVariable.scheduler == "RR")
         {
            //Run the program in first in first out order
            while (appQueue.Count != 0)        //While the queue is NOT empty we still need 
            {
               // reset and start the timer
               realTime.Reset();
               realTime.Start();
               Thread.Sleep(1);    //test for interupt (1millisecond)

               int currAppIndex = appQueue.Dequeue(); // dequeue the first index from the queue of applications
               int currProcIndex = findIndexOfNextProc(ourAppList[currAppIndex]); // Find the index of the next process to run within this application, Returns -1 if all processes for this application are complete
               int currIOIndex = -1;

               // if the current process of the current application is complete, the IO for that process gets handled
               if (ourAppList[currAppIndex].procList[currProcIndex].remCT == 0)
               {
                  //Handle IO Here
                  Thread.Sleep(1);    //test for interupt (1millisecond)
                  currIOIndex = findIndexOfNextIO(ourAppList[currAppIndex].procList[currProcIndex]);  //Find next IO to handle
                  ourAppList[currAppIndex].procList[currProcIndex].ioList[currIOIndex].hasStarted = true; // set the bool to true so that the OS knows the IO has started
                  ourAppList[currAppIndex].procList[currProcIndex].ioList[currIOIndex].beginTime = GlobalVariable.clockTime;
                  ourAppList[currAppIndex].procList[currProcIndex].ioList[currIOIndex].endTime = (GlobalVariable.clockTime + ourAppList[currAppIndex].procList[currProcIndex].ioList[currIOIndex].initCT);
                  Thread.Sleep(1);    //test for interupt (1millisecond)

                  // let the user know the IO is started and that the system is swapping processes
                  realTime.Stop();
                  microSec = calcMicroSec(realTime.ElapsedTicks);
                  log("SYSTEM – Managing I/O  (" + realTime.ElapsedMilliseconds + " mSec | " + microSec + " \u00b5Sec)");
                  log("PID " + (currAppIndex + 1) + " - " + ourAppList[currAppIndex].procList[currProcIndex].ioList[currIOIndex].type + ", " + ourAppList[currAppIndex].procList[currProcIndex].ioList[currIOIndex].descriptor + " started");

                  // this is to check if the IO we were just working on has completed
                  Thread.Sleep(1000);    //test for interupt (1millisecond)
                  Thread thread = new Thread(() => startThread(ourAppList[currAppIndex], ourAppList[currAppIndex].procList[currProcIndex].ioList[currIOIndex])); ;
                  thread.Start();
                  Thread.Sleep(1000);    //test for interupt (1second)
                  currIOIndex = findIndexOfNextIO(ourAppList[currAppIndex].procList[currProcIndex]);

                  // if it has, set the IO's bool to true so we know its done
                  if (currIOIndex == -1)
                  {
                     ourAppList[currAppIndex].procList[currProcIndex].finished = true;
                  }
               }

               // if the current process hasn't completed we have to process it based on the quantum
               else
               {
                  // this is to track how many cycles have gone by and to compare it to the quantum
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

                  // output to the user how long the process ran
                  int procTime = cyclesRan * GlobalVariable.procTime;
                  log("PID " + (currAppIndex + 1) + " - Processing (" + procTime + " mSec)");

                  // if the remCT is now zero for this process we know that it is completed
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

               // this is to check for any IO threads running for the process
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
               }

               // if there is still information in the AppQueue, let the user know we are swapping processes
               if (appQueue.Count > 0)
               {
                  realTime.Stop();
                  microSec = calcMicroSec(realTime.ElapsedTicks);
                  log("SYSTEM – Swapping processes (" + realTime.ElapsedMilliseconds + " mSec | " + microSec + " \u00b5Sec)");
               }
            }
            // reset and start the clock again
            realTime.Reset();
            realTime.Start();
         }

         // since all the processing is done stop the clock
         realTime.Stop();

         // if the config file specified to log to a file, do it now
         if (GlobalVariable.logToFile)
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

         // ask for input from the user so the screen won't automatically shut off. When input is given end the program.
         Console.ReadKey();
         Environment.Exit(0);

      }
   }
}