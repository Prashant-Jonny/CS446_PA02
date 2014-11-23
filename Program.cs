using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace simulator
{
   // Global Variables
   public static class GlobalVariable
   {
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
      public static int clockTime;
   }

   // Application Class
   class Application
   {
      public int PIDNum;
      public int numProc;
      public int procRem;
      public List<Process> procList = new List<Process>();
      public bool finished = false;

      public void sortSJF()
      {
         this.procList = procList.OrderBy(o=>o.initCT).ToList();
      }

      public void sortSRTF()
      {
         this.procList = procList.OrderBy(o => o.remCT).ToList();
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
      public char type;
      public string descriptor;
   }

   // Main Program
   class Program
   {
      /*-----------
        Functions
       ----------*/ 

      static int findCycleTime(string s)      //Determines cycle time in our meta-data string
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

      static string findDescriptor(string s) //Determines string value between ()'s in meta-data string
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

      static void copyProcess(Process src, Process dest)
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

      static void copyApplication(Application src, Application dest)
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

      static void Main(string[] args)
      {
         /*-----------
           Variables 
         -----------*/

         string configFile, configPath;                 
         bool appStarted = false, procStarted = false;
         int appIndex = 0, procIndex = 0, tempInt = 0;            // Application and process index variables
         List<string> ourData = new List<string>();               // Temporary list to hold all meta data
         List<Application> ourAppList = new List<Application>();  // Our application list

         Application tempApp = new Application();
         Process tempProc = new Process();

         //string dataFile = File.ReadAllText(@"C:\Users\team8\Desktop\exampleFile.txt");//Used on cpe lab comp
         string dataFile = "S(start)0; A(start)0; P(run)13; I(keyboard)5; P(run)6; O(monitor)5; P(run)5; I(hard drive)5; P(run)7; A(end)0; A(start)0; P(run)10; I(keyboard)5; P(run)7; O(hard drive)5; P(run)15; A(end)0; A(start)0; P(run)13; I(hard drive)5; P(run)14; O(hard drive)5; P(run)13; I(hard drive)5; P(run)10; S(end)0.";
         //ABOVE IS AN EXAMPLE^ WE WILL FIX LATER

         /*-----------------------------------------------------------
           Read in the configuration file from command-line argument 
         -----------------------------------------------------------*/

         // Checks if user had input an argument, if not, exit the program
         if (args.Length == 0)
         {
            Console.WriteLine("Error: Please enter the name of the configuration file. \nPress a key to exit.");
            Console.ReadKey();
            Environment.Exit(1);
         }

         // If there is a valid argument, start reading in data
         else
         {
            // Get the configuration file name
            configFile = args[0];
            configPath = Directory.GetCurrentDirectory();   // This is the current directory of the running program for our purposes, 
                                                            // PUT FILES HERE, MIGHT CHANGE LATER
            //Console.WriteLine(configPath);                // TEST
            //Console.WriteLine(configFile);
            // Read in all the lines to an array, prints an error if an exception is caught
            try
            {
               string[] lines = File.ReadAllLines(configFile);
               int index;

               // Gets the right side of the semi-colon and replaces the full lines in the previous array
               for (index = 0; index < lines.Length; index++)
               {
                  if (lines[index].Contains(":"))
                  {
                     lines[index] = lines[index].Split(':')[1];
                     // Gets rid of white-space
                     lines[index] = lines[index].Trim();

                     //Console.WriteLine(lines[index]);

                  }
               }

               // Update Global Variables
               GlobalVariable.version = lines[1];
               GlobalVariable.quantum = int.Parse(lines[2]);
               GlobalVariable.scheduler = lines[3];
               GlobalVariable.filePath = configPath;
               GlobalVariable.procTime = int.Parse(lines[5]);
               GlobalVariable.monitorTime = int.Parse(lines[6]);
               GlobalVariable.hdTime = int.Parse(lines[7]);
               GlobalVariable.printerTime = int.Parse(lines[8]);
               GlobalVariable.kbTime = int.Parse(lines[9]);
               GlobalVariable.memType = lines[10];
               GlobalVariable.log = lines[11];

               /* TESTING GLOBAL VARIABLES
               Console.WriteLine(GlobalVariable.version);
               Console.WriteLine(GlobalVariable.quantum);
               Console.WriteLine(GlobalVariable.scheduler);
               Console.WriteLine(GlobalVariable.filePath);
               Console.WriteLine(GlobalVariable.procTime);
               Console.WriteLine(GlobalVariable.monitorTime);
               Console.WriteLine(GlobalVariable.hdTime);
               Console.WriteLine(GlobalVariable.printerTime);
               Console.WriteLine(GlobalVariable.kbTime);
               Console.WriteLine(GlobalVariable.memType);
               Console.WriteLine(GlobalVariable.log); */

            }

            catch
            {
               Console.WriteLine("Error: Problems reading the configuration file. \nPress a key to exit.");
               Console.ReadKey();
               Environment.Exit(1);
            }
         }

         /*----------------------------------------------------
           Read in the meta-data to their respectable classes
         ----------------------------------------------------*/ 

         string x = dataFile;
         int index1 = 0, index2 = x.IndexOf(';');
         while (index2 != -1)
         {
            int q = index2 - index1;
            //Console.WriteLine(x.Substring(index1, q));//Debugging
            string temp = x.Substring(index1, q);
            temp = temp.Trim();
            ourData.Add(temp);
            index1 = index2 + 1;
            index2 = x.IndexOf(';', index2 + 1);
         }
         string temp2 = x.Substring(index1);
         temp2 = temp2.Trim();
         ourData.Add(temp2);//Adds the final System End

         //Begin to store data in proper structures (ourAppList)
         foreach (string currentLine in ourData)
         {
            //Console.WriteLine(currentLine);//Debugging (Displays all elements stored in our list)
            switch (currentLine[0])
            {
               // Operating System Operations, Start & End
               case 'S':
                  {
                     if (findDescriptor(currentLine) == "start")
                     {
                        Console.WriteLine("SYSTEM - Boot, set up (Need to calculate time, somehow)");
                     }

                     // If this is the end of the meta-data, make sure everything has been added in
                     else
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
                     if (findDescriptor(currentLine) == "start")
                     {
                        appStarted = true;
                        tempApp.procList.Clear();
                        appIndex++;                                        // Increment number of applications
                        tempApp.PIDNum = appIndex;
                        Console.WriteLine("PID " + appIndex + " - Enter system");
                        Console.WriteLine("SYSTEM - Creating PID " + appIndex + " (TIME)");
                     }

                     // If this is the end of an application, store values into list
                     else                                                  
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
                        appStarted = false;
                     }
                     break;
                  }

               // Process Operations, Run
               case 'P':
                  {
                     // Copy
                     if (procStarted)
                     {
                        Process tempProc2 = new Process();
                        copyProcess(tempProc, tempProc2);
                        tempApp.procList.Add(tempProc2);
                        procStarted = false;
                     }

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
                     IO tempIO = new IO();
                     tempInt = findCycleTime(currentLine);
                     tempIO.initCT = tempInt;
                     tempIO.remCT = tempInt;
                     tempIO.type = 'I';
                     tempIO.descriptor = findDescriptor(currentLine);
                     tempProc.ioList.Add(tempIO);        //Add to the last current running process
                     break;
                  }

               // Output Operations, Hard Drive & Monitor
               case 'O':
                  {
                     IO tempIO = new IO();
                     tempInt = findCycleTime(currentLine);
                     tempIO.initCT = tempInt;
                     tempIO.remCT = tempInt;
                     tempIO.type = 'O';
                     tempIO.descriptor = findDescriptor(currentLine);
                     tempProc.ioList.Add(tempIO);        //Add to the last current running process
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

         // Check if all data was read in correctly
         foreach (Application a in ourAppList)
         {
            Console.WriteLine("PID #" + a.PIDNum);
            Console.WriteLine("NumProc" + a.numProc);
            Console.WriteLine("ProcRem" + a.procRem);
            for (tempInt = 0; tempInt < a.procList.Count; tempInt++)
            {
               Console.WriteLine("\t" + "Init Ct @ " + tempInt + " = " + a.procList[tempInt].initCT);
               Console.WriteLine("\t" + "Rem  Ct @ " + tempInt + " = " + a.procList[tempInt].remCT);
               for (int abc = 0; abc < a.procList[tempInt].ioList.Count; abc++)
               {
                  Console.WriteLine("\t" + "\t" + "\t" + a.procList[tempInt].ioList[abc].initCT);
                  Console.WriteLine("\t" + "\t" + "\t" + a.procList[tempInt].ioList[abc].remCT);
                  Console.WriteLine("\t" + "\t" + "\t" + a.procList[tempInt].ioList[abc].type);
                  Console.WriteLine("\t" + "\t" + "\t" + a.procList[tempInt].ioList[abc].descriptor);
               }
            }
         }

         if (GlobalVariable.scheduler == "SJF")
         {
            // for each application, the list of processes will be sorted from least to greatest based on remCT
         }


         else if (GlobalVariable.scheduler == "SRTF")
         {
            // for each application, the list of processes will be sorted from least to greatest based on remCT
         }

         bool allAppsFinished = false;
         int currentApp;

         //Start main program
         while (allAppsFinished != true)
         {

         }

         Console.ReadKey();
         Environment.Exit(0);

      }
   }
}
