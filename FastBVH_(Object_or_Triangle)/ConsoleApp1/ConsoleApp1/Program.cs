using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK;

namespace ConsoleApp1
{
    class Vector3SortX : IComparer<Vector3>
    {
        public int Compare(Vector3 a, Vector3 b)
        {
            if (a.X < b.X) return -1;
            if (a.X > b.X) return 1;
            return 0;
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            int NUM_OBJECTS_ALL = 35000;
            int NUM_THREADS = 64;
            int NUM_OBJECTS_ONETHREAD = (int)((float)NUM_OBJECTS_ALL / (float)NUM_THREADS);
            int step = 20;

            List<Vector3> list0 = new List<Vector3>();
            for (int i = 0; i < NUM_OBJECTS_ALL; i++) 
            {
                list0.Add(new Vector3(i, 0, 0));
            }

            int fps = 0;
            DateTime elapsedTime = DateTime.Now;
            DateTime currTime = DateTime.Now;

            List<List<Vector3>> parallelList = new List<List<Vector3>>();

            while (true)
            {
                currTime = DateTime.Now;
                fps++;
                if ((currTime - elapsedTime).TotalSeconds >= 1.0f) 
                {
                    System.Console.WriteLine("FPS: " + (fps / 1.0f));
                    fps = 0;
                    elapsedTime = currTime;
                }

                list0.Sort(new Vector3SortX());

                
                parallelList.Clear();
                for (int t = 0; t < NUM_THREADS; t++)
                {
                    Vector3[] threadList = new Vector3[NUM_OBJECTS_ONETHREAD];
                    list0.CopyTo(NUM_OBJECTS_ONETHREAD * t, threadList, 0, NUM_OBJECTS_ONETHREAD);
                    parallelList.Add(threadList.ToList());
                }


                

                Parallel.ForEach(parallelList, (list) => 
                {
                    float minDist;
                    float currDist;
                    int id = 0;
                    int i;

                    List<Vector3> inBuffer = new List<Vector3>();
                    List<Vector3> outBuffer = new List<Vector3>();

                    outBuffer.AddRange(list);

                    while (outBuffer.Count > 1)
                    {
                        inBuffer.Clear();
                        inBuffer.AddRange(outBuffer);

                        outBuffer.Clear();

                        while (inBuffer.Count > 1)
                        {
                            minDist = float.MaxValue;

                            for (i = 1; i <= step; i++)
                            {
                                if (i >= inBuffer.Count) { continue; }

                                currDist = Vector3.Distance(list[0], list[i]);
                                if (currDist < minDist)
                                {
                                    minDist = currDist;
                                    id = i;
                                }
                            }

                            // node keszitese
                            outBuffer.Add(inBuffer[0]);
                            //outBuffer.Add(inBuffer[id]);

                            inBuffer.RemoveAt(id);
                            inBuffer.RemoveAt(0);
                        }

                        if (inBuffer.Count == 1)
                        {
                            outBuffer.Add(inBuffer[0]);
                            inBuffer.RemoveAt(0);
                        }

                    }

                    Vector3 root = outBuffer[0];
                    outBuffer.RemoveAt(0);
                });
                
            }
        }
    }
}
