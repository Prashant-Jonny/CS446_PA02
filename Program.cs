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
   }

   // Process Class
   class Process
   {
      public int initCT;
      public int remCT;
      public List<IO> ioList = new List<IO>();
      public bool finished = false;
   }

   // I/O Class
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
      static void Main(string[] args)
      {
         //Declare all variables
         char lastOp = 'S';                                       // Stores the value of last operation
         string configFile, configPath, fullPath;
         bool appStarted = false, procStarted = false;
         int appIndex = 0, procIndex = 0, tempInt = 0;            // Application and process index variables

         //Declaring Lists!
         List<string> ourData = new List<string>();               //Temporary list to hold all meta data
         List<Application> ourAppList = new List<Application>();
         Application tempApp = new Application();
         Process tempProc = new Process();
         IO tempIO = new IO();

         //
         // Read in the configuration file from command-line argument
         //
         // Checks if user had input an argument, if not exit the program
         if( args.Length == 0 )
         {
            Console.WriteLine("Please enter the name of the configuration file.");
            Environment.Exit(1);
         }
         // If there is a valid argument, start reading in data
         else
         {
            // Get the configuration file name
            configFile = args[0];
            configPath = Directory.GetCurrentDirectory();  // This is the current directory of the running program for our purposes, 
                                                           // PUT FILES HERE, MIGHT CHANGE LATER
            //Console.WriteLine(configPath);                 // TEST
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
                     lines[index] = lines[index].TrimStart();

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

               /*
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
               Console.WriteLine(GlobalVariable.log);
               */

            }

            catch
            {
               Console.WriteLine("An error occured while reading the configuration file. Aborting program.");
               Environment.Exit(1);
            }
         }

         //string dataFile = File.ReadAllText(@"C:\Users\team8\Desktop\exampleFile.txt");//Used on cpe lab comp
         string dataFile = "S(start)0; A(start)0; P(run)13; I(keyboard)5; P(run)6; O(monitor)5; P(run)5; I(hard drive)5; P(run)7; A(end)0; A(start)0; P(run)10; I(keyboard)5; P(run)7; O(hard drive)5; P(run)15; A(end)0; A(start)0; P(run)13; I(hard drive)5; P(run)14; O(hard drive)5; P(run)13; I(hard drive)5; P(run)10; S(end)0.";
         //ABOVE IS AN EXAMPLE^ WE WILL FIX LATER

         //Prime the data read in loop-------------------------
         string x = dataFile;
         int index1 = 0, index2 = x.IndexOf(';');
         while (index2 != -1)
         {
               int q = index2 - index1;
               //Console.WriteLine(x.Substring(index1, q));//Debugging
               string temp = x.Substring(index1, q);
               ourData.Add(temp);
               index1 = index2 + 1;
               index2 = x.IndexOf(';', index2 + 1);
         }
         string temp2 = x.Substring(index1);
         ourData.Add(temp2);//Adds the final System End
         //End of data read in loop (to a list of strings)-----

         //Begin to store data in proper 
         foreach (string currentLine in ourData)
         {
               //Console.WriteLine(currentLine);//Debugging (Displays all elements stored in our list)
               if (currentLine[0] == 'S' || currentLine[1] == 'S') //Handling System Operation
               {
                  //Console.Write('S');
                  lastOp = 'S';
               }

               if (currentLine[0] == 'A' || currentLine[1] == 'A') //Handling Application Operation
               {
                  //Console.Write('A');
                  if (appStarted)         //If this is the end of an application, simply change bool value
                  {
                     tempInt = tempApp.procList.Count;
                     tempApp.numProc = tempInt;
                     tempApp.procRem = tempInt;
                     ourAppList.Add(tempApp);
                     //Delete tempApp to prep for a new on
                     appStarted = false;
                  }
                  else                    //Otherwise begin setting data for application
                  {
                     appStarted = true;
                     tempApp.procList = new List<Process>();
                     tempApp.PIDNum = appIndex;
                     appIndex++;     //Increment number of applications

                     //ourAppList.Add
                     lastOp = 'A';
                  }
               }

               if (currentLine[0] == 'P' || currentLine[1] == 'P') //Handling Process Operation
               {
                  //Console.Write('P');
                  int len = currentLine.Length;
                  tempInt = 0;
                  for (int i = 0; i < len; i++)
                  {
                     if (currentLine[i] == ')' && (i + 2) < len)
                     {
                           tempInt = (int)(currentLine[i + 1] - 48) * 10;
                           tempInt += (int)(currentLine[i + 2] - 48);
                           //Console.WriteLine(tempInt);
                     }
                     else if (currentLine[i] == ')')
                     {
                           tempInt = (int)(currentLine[i + 1] - 48);
                           //Console.WriteLine(tempInt);
                     }
                     tempProc.initCT = tempInt;
                     tempProc.remCT = tempInt;
                     tempProc.ioList = new List<IO>();
                     //USE TEMPINT TO STORE INITCT FOR PROCESSES AND IO
                  }
                  lastOp = 'P';
               }

               if (currentLine[0] == 'I' || currentLine[1] == 'I') //Handling Input Operation
               {
                  //Console.Write('I');
                  int len = currentLine.Length;
                  tempInt = 0;
                  for (int i = 0; i < len; i++)
                  {
                     if (currentLine[i] == ')' && (i + 2) < len)
                     {
                           tempInt = (int)(currentLine[i + 1] - 48) * 10;
                           tempInt += (int)(currentLine[i + 2] - 48);
                           //Console.WriteLine(tempInt);
                     }
                     else if (currentLine[i] == ')')
                     {
                           tempInt = (int)(currentLine[i + 1] - 48);
                           //Console.WriteLine(tempInt);
                     }
                     //USE TEMPINT TO STORE INITCT FOR PROCESSES AND IO
                  }
                  lastOp = 'I';
               }

               if (currentLine[0] == 'O' || currentLine[1] == 'O') //Handling Output Operation
               {
                  //Console.Write('O');
                  int len = currentLine.Length;
                  tempInt = 0;
                  for (int i = 0; i < len; i++)
                  {
                     if (currentLine[i] == ')' && (i + 2) < len)
                     {
                           tempInt = (int)(currentLine[i + 1] - 48) * 10;
                           tempInt += (int)(currentLine[i + 2] - 48);
                           //Console.WriteLine(tempInt);
                     }
                     else if (currentLine[i] == ')')
                     {
                           tempInt = (int)(currentLine[i + 1] - 48);
                           //Console.WriteLine(tempInt);
                     }
                     //USE TEMPINT TO STORE INITCT FOR PROCESSES AND IO
                  }
                  lastOp = 'O';
               }
         }

         //Start main program
         Console.ReadKey();

      }
   }
}
