using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace ProjTransakcje
{
    class Program
    {

        static void Main(string[] args)
        {
            Transakcja[] transakcje = new Transakcja[100];
            Optimistic[] optimistic = new Optimistic[100];

            Thread[] t = new Thread[100];

            //for(int i=0; i<transakcje.Length; i++)
            //{
            //    transakcje[i] = new Transakcja();
            //    t[i] = new Thread(transakcje[i].Start);
            //}

            //for (int i = 0; i < t.Length; i++)
            //{
            //    t[i].Start();
            //}

            //for (int i = 0; i < 10; i++)
            //{
            //    t[i].Join();
            //}

            ////////////OPTIMISTIC////////////////////////

            for (int i = 0; i < optimistic.Length; i++)
            {
                optimistic[i] = new Optimistic();
                t[i] = new Thread(optimistic[i].TransakcjaOptimistic);
            }

            for (int i = 0; i < t.Length; i++)
            {
                t[i].Start();
            }

            for (int i = 0; i < 10; i++)
            {
                t[i].Join();
            }



            Console.ReadKey();

        }
    }
}
