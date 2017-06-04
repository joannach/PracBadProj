using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;

namespace ProjTransakcje
{
    /// <summary>
    /// Klasa przedstawia model PESIMISTIC!!
    /// </summary>
    class Transakcja
    {
        public int t_liczba_miejsc = 51;
        int t_id = 1;
        public Wynik t_wynik;

        public SqlConnection m_connection;

        public string connectionString = "Data Source = DESKTOP - K6OJ71S; Initial Catalog = CINEMA;" + "Integrated Security = true";

        public MetodyWyniki daj_metode = new MetodyWyniki();

        public int[] wolne_miejsca_insert(SqlTransaction transaction)
        {
            bool[] wolne = new bool[t_liczba_miejsc];

            for (int i = 0; i < wolne.Length; i++)
                wolne[i] = true;

            string queryString = "select NrMiejsca from Zamowienie";

            SqlCommand command1 = new SqlCommand(queryString, m_connection, transaction);


            SqlDataReader dataReader1 = command1.ExecuteReader();
            while (dataReader1.Read())
            {
                int nr_miejsca = (int)dataReader1["NrMiejsca"];
                wolne[nr_miejsca] = false;
            }

            dataReader1.Close();

            int ile_wolnych = 0;
            for(int i=1; i<wolne.Length; i++)
            {
                if (wolne[i] == true)
                    ile_wolnych++;
            }

            if (ile_wolnych == 0)
                return null;

            int[] numery_wolnych = new int[ile_wolnych];
            int j = 0;
            for (int i = 1; i < wolne.Length; i++)
            {
                if (wolne[i] == true)
                    numery_wolnych[j++] = i;
            }
            return numery_wolnych;
        }

        public int[] wolne_miejsca_update(SqlTransaction transaction)
        {
            bool[] wolne = new bool[t_liczba_miejsc];

            for (int i = 1; i < wolne.Length; i++)
                wolne[i] = true;

            string queryString = "select NrMiejsca from Zamowienie where StatusB = 0;";      // 0 - wolny

            SqlCommand command1 = new SqlCommand(queryString, m_connection, transaction);


            SqlDataReader dataReader1 = command1.ExecuteReader();
            while (dataReader1.Read())
            {
                int nr_miejsca = (int)dataReader1["NrMiejsca"];
                wolne[nr_miejsca] = false;
            }

            dataReader1.Close();

            int ile_wolnych = 0;
            for (int i = 1; i < wolne.Length; i++)
            {
                if (wolne[i] == true)
                    ile_wolnych++;
            }

            if (ile_wolnych == 0)
                return null;

            int[] numery_wolnych = new int[ile_wolnych];
            int j = 0;
            for (int i = 1; i < wolne.Length; i++)
            {
                if (wolne[i] == true)
                    numery_wolnych[j++] = i;
            }
            return numery_wolnych;
        }

        public void Start()
        {
            t_wynik = new Wynik();

            using (m_connection = new SqlConnection(connectionString))
            {
                m_connection.Open();

                int[] wolne = wolne_miejsca_update(null);

                if (wolne == null)
                {
                    Console.WriteLine("Brak wolnych ");
                    return;
                }
                Random r = new Random();
                int miejsce = r.Next(1, wolne.Length);

                System.Threading.Thread.Sleep(r.Next(1000, 5001));

                SqlCommand command_insert = m_connection.CreateCommand();
                SqlCommand command_update = m_connection.CreateCommand();
                SqlTransaction transaction;

                t_wynik.w_start = DateTime.Now;                      //czas rozpoczęcia
                // Start a local transaction.
                transaction = m_connection.BeginTransaction("SampleTransaction");
                Console.WriteLine("Rozpoczecie transakcji dla miejsca {0}: ", miejsce);

                // Must assign both transaction object and connection
                // to Command object for a pending local transaction
                command_update.Connection = m_connection;
                command_update.Transaction = transaction;

                wolne = wolne_miejsca_update(transaction);

                if (wolne == null)
                {
                    Console.WriteLine("Brak wolnych");                    
                    transaction.Rollback();
                    //info 
                }
                else
                {
                    int czas_op = r.Next(1000, 5000);
                    System.Threading.Thread.Sleep(czas_op);


                        if (wolne.Contains(miejsce))
                        {
                            command_insert.CommandText = "Insert into Zamowienie (IDZamowienia, IDSeansu, NrMiejsca, StatusB, NazwaKlienta) VALUES (" + t_id + "3, 1, " + miejsce + ", 1, 'nazwakli')";
                            command_update.CommandText = "UPDATE Zamowienie SET StatusB = '0' WHERE IDZamowienia = " + t_id + "; ";
                            command_update.ExecuteNonQuery();
                            Console.WriteLine("Transakcja miejsca {0} na poziomie izolacji: {1}", miejsce, transaction.IsolationLevel.ToString());
                            transaction.Commit();
                            Console.WriteLine("Skommitowano transakcję dla miejsca: {0}", miejsce);
                        }
                    
                   else
                    {
                        //info ze w miedzyczasie zost sprzedane
                        Console.WriteLine("Transakcja dla miejsca {0} nie doszla do skutku,", miejsce);
                        transaction.Rollback();
                        t_wynik.w_konflikt = true;
                    }
                    t_wynik.w_stop = DateTime.Now;
                    TimeSpan czas = t_wynik.w_stop - t_wynik.w_start;
                    t_wynik.w_opoznienie = daj_metode.policzOpoznienie(czas, miejsce);

                    // tu od timera odjac randomowy czas opoznienia 
                    Console.WriteLine("Czas transakcji dla miejsca {0} wynosi: {1}", miejsce, czas.TotalMilliseconds );//timer.ElapsedMilliseconds.ToString());
                }
                daj_metode.dodaj_wynik(t_wynik, m_connection);
            }
        }

    }
}


//w sqlserver adhoc MS - 2 roznoleglee sesje, na odczyt i update
//tryb obsługi konfliktu blokad
//jesli jest blokada to odczytac czas przed i po wysklaniu operacji - na operacji update
//jesli konflikt - scenariusz alternatywny, moze w petli - policzyc ile nieudanych

    //optimistic - odczytac liczbe zmodyfikowanych krotek
    // kazda operacja w osobnej transakcji

