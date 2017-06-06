using System;
using System.Data.SqlClient;
using System.Linq;

namespace ProjTransakcje
{
    class Optimistic
    {
        /// <summary>
        /// Calkowita liczba miejsc w sali
        /// </summary>
        public int t_liczba_miejsc = 51;

        /// <summary>
        /// ID Zamowienia
        /// </summary>
        int t_id = 1;

        /// <summary>
        /// Klasa - odwzorowanie tabeli z wynikami z bazy
        /// </summary>
        public Wynik t_wynik;

        /// <summary>
        /// losowana liczba miejsc
        /// </summary>
        int[] miejsca; 

        public SqlConnection m_connection;
        public SqlConnection m_connection_insert;
        public SqlConnection m_connection_update;

        public string connectionString = "Data Source=DESKTOP-K6OJ71S;Initial Catalog=CINEMA;" + "Integrated Security=true";

        public MetodyWyniki daj_metode = new MetodyWyniki();


        /// <summary>
        /// Metoda ogarniajaca te wszystkie tablice z miejscami
        /// </summary>
        /// <param name="transaction"></param>
        /// <returns>NUMERY WOLNYCH MIEJSC</returns>
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
                wolne[nr_miejsca] = false;                              // false - wolne
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

        public void TransakcjaOptimistic()
        {
            t_wynik = new Wynik();

            int[] wolne;
            int miejsce;


            Random r = new Random();

            using (m_connection = new SqlConnection(connectionString))
            {
                m_connection.Open();

                wolne = wolne_miejsca_update(null);               //TU PIERWSZA TRANSAKCJA

                if (wolne == null)
                {
                    Console.WriteLine("Brak wolnych ");
                    return;
                }

                miejsce = r.Next(1, wolne.Length);

                System.Threading.Thread.Sleep(r.Next(1000, 5001));

                SqlCommand command_select = m_connection.CreateCommand();
                SqlTransaction transaction_select;


                t_wynik.w_start = DateTime.Now;                      //czas rozpoczęcia
                // Start a local transaction.
                transaction_select = m_connection.BeginTransaction("SampleTransaction");
                Console.WriteLine("Rozpoczecie transakcji dla selecta ");

                // Must assign both transaction object and connection
                // to Command object for a pending local transaction
                command_select.Connection = m_connection;
                command_select.Transaction = transaction_select;                       //TRANSAKCJA DLA PIERWSZEGO SELECTA

                wolne = wolne_miejsca_update(transaction_select);

                if (wolne == null)
                {
                    Console.WriteLine("Brak wolnych");
                    transaction_select.Rollback();
                    //info 
                }
                else
                {
                    int czas_op = r.Next(1000, 5000);
                    System.Threading.Thread.Sleep(czas_op);


                    if (wolne.Contains(miejsce))
                    {
                        command_select.CommandText = "select NrMiejsca from Zamowienie where StatusB = 0;";
                        wolne = wolne_miejsca_update(transaction_select);
                        command_select.ExecuteNonQuery();
                        Console.WriteLine("Transakcja selecta na poziomie izolacji: {0}", transaction_select.IsolationLevel.ToString());
                        transaction_select.Commit();
                        Console.WriteLine("Skommitowano transakcję selecta dla miejsca");
                    }

                    else
                    {
                        //info ze w miedzyczasie zost sprzedane
                        Console.WriteLine("Transakcja selecta nie doszla do skutku");
                        transaction_select.Rollback();
                        t_wynik.w_konflikt = true;
                    }
                    t_wynik.w_stop = DateTime.Now;
                    TimeSpan czas = t_wynik.w_stop - t_wynik.w_start;
                    t_wynik.w_opoznienie = daj_metode.policzOpoznienie(czas, miejsce);

                    // tu od timera odjac randomowy czas opoznienia 
                    // Console.WriteLine("Czas transakcji selecta wynosi: {1}", czas.TotalMilliseconds);//timer.ElapsedMilliseconds.ToString());
                }
                daj_metode.dodaj_wynik(t_wynik, m_connection);
            }

            using (m_connection_insert = new SqlConnection(connectionString))
            {
                m_connection_insert.Open();
                SqlCommand command_insert = m_connection_insert.CreateCommand();
                SqlTransaction transaction_insert;

                ///////////////////////////druga transakcja///////////////////////////////////////
                transaction_insert = m_connection_insert.BeginTransaction("InsertTransaction");
                command_insert.Connection = m_connection_insert;

                command_insert.Connection = m_connection_insert;
                command_insert.Transaction = transaction_insert;

                if (wolne == null)
                {
                    Console.WriteLine("Brak wolnych");
                    transaction_insert.Rollback();
                    //info 
                }
                else
                {
                    int czas_op = r.Next(1000, 5000);
                    System.Threading.Thread.Sleep(czas_op);


                    if (wolne.Contains(miejsce))
                    {
                        command_insert.CommandText = "Insert into Zamowienie (IDSeansu, NrMiejsca, StatusB, NazwaKlienta) VALUES (1, " + miejsce + ", 1, 'nazwakli')";
                        command_insert.ExecuteNonQuery();
                        
                        Console.WriteLine("Transakcja insert miejsca {0} na poziomie izolacji: {1}", miejsce, transaction_insert.IsolationLevel.ToString());
                        transaction_insert.Commit();
                        Console.WriteLine("Skommitowano transakcję insert dla miejsca: {0}", miejsce);
                    }

                    else
                    {
                        //info ze w miedzyczasie zost sprzedane
                        Console.WriteLine("Transakcja insert dla miejsca {0} nie doszla do skutku,", miejsce);
                        transaction_insert.Rollback();
                        t_wynik.w_konflikt = true;
                    }
                    t_wynik.w_stop = DateTime.Now;
                    TimeSpan czas = t_wynik.w_stop - t_wynik.w_start;
                    t_wynik.w_opoznienie = daj_metode.policzOpoznienie(czas, miejsce);

                    // tu od timera odjac randomowy czas opoznienia 
                    Console.WriteLine("Czas transakcji insert dla miejsca {0} wynosi: {1}", miejsce, czas.TotalMilliseconds);//timer.ElapsedMilliseconds.ToString());
                }

                daj_metode.dodaj_wynik(t_wynik, m_connection_insert);
            }

            //using (m_connection_update = new SqlConnection(connectionString))
            //{
            //    m_connection_update.Open();
            //    SqlCommand command_update = m_connection_update.CreateCommand();
            //    SqlTransaction transaction_update;

            //    transaction_update = m_connection_update.BeginTransaction("UpdateTransaction");
            //    command_update.Connection = m_connection_update;

            //    command_update.Connection = m_connection_update;
            //    command_update.Transaction = transaction_update;

            //    if (wolne == null)
            //    {
            //        Console.WriteLine("Brak wolnych");
            //        transaction_update.Rollback();
            //        //info 
            //    }
            //    else
            //    {
            //        int czas_op = r.Next(1000, 5000);
            //        System.Threading.Thread.Sleep(czas_op);

            //        if (wolne.Contains(miejsce))
            //        {
            //            command_update.CommandText = "UPDATE Zamowienie SET StatusB = '0' WHERE IDZamowienia = " + t_id + "; ";
            //            command_update.ExecuteNonQuery();
            //            Console.WriteLine("Transakcja update miejsca {0} na poziomie izolacji: {1}", miejsce, transaction_update.IsolationLevel.ToString());

            //            transaction_update.Commit();
            //            Console.WriteLine("Skommitowano transakcję update dla miejsca: {0}", miejsce);
            //        }
            //        else
            //        {
            //            //info ze w miedzyczasie zost sprzedane
            //            Console.WriteLine("Transakcja update dla miejsca {0} nie doszla do skutku,", miejsce);
            //            transaction_update.Rollback();
            //            t_wynik.w_konflikt = true;
            //        }

            //        t_wynik.w_stop = DateTime.Now;
            //        TimeSpan czas = t_wynik.w_stop - t_wynik.w_start;
            //        t_wynik.w_opoznienie = daj_metode.policzOpoznienie(czas, miejsce);

            //        // tu od timera odjac randomowy czas opoznienia 
            //        Console.WriteLine("Czas transakcji update dla miejsca {0} wynosi: {1}", miejsce, czas.TotalMilliseconds);//timer.ElapsedMilliseconds.ToString());

            //    }
            //    daj_metode.dodaj_wynik(t_wynik, m_connection_update);
            //}



            ///////////////////////////////////////dla wielu//////////////////////////////////

            miejsca = new int[r.Next(4)];

            for(int i=0; i<miejsca.Length; i++)
            {
                miejsca[i] = r.Next(1, wolne.Length);

                using (m_connection_update = new SqlConnection(connectionString))
                {
                    m_connection_update.Open();
                    SqlCommand command_update = m_connection_update.CreateCommand();
                    SqlTransaction transaction_update;

                    transaction_update = m_connection_update.BeginTransaction("UpdateTransaction");
                    command_update.Connection = m_connection_update;

                    command_update.Connection = m_connection_update;
                    command_update.Transaction = transaction_update;

                    if (wolne == null)
                    {
                        Console.WriteLine("Brak wolnych");
                        transaction_update.Rollback();
                        //info 
                    }
                    else
                    {
                        int czas_op = r.Next(1000, 5000);
                        System.Threading.Thread.Sleep(czas_op);

                        if (wolne.Contains(miejsca[i]))
                        {
                            int m = miejsca[i];
                            command_update.CommandText = "UPDATE Zamowienie SET StatusB = '0' WHERE NrMiejsca = " + m + "; ";
                            command_update.ExecuteNonQuery();
                            Console.WriteLine("Transakcja update miejsca {0} na poziomie izolacji: {1}", miejsca[i], transaction_update.IsolationLevel.ToString());

                            transaction_update.Commit();
                            Console.WriteLine("Skommitowano transakcję update dla miejsca: {0}", miejsca[i]);
                        }
                        else
                        {
                            //info ze w miedzyczasie zost sprzedane
                            Console.WriteLine("Transakcja update dla miejsca {0} nie doszla do skutku,", miejsca[i]);
                            transaction_update.Rollback();
                            t_wynik.w_konflikt = true;
                        }

                        t_wynik.w_stop = DateTime.Now;
                        TimeSpan czas = t_wynik.w_stop - t_wynik.w_start;
                        t_wynik.w_opoznienie = daj_metode.policzOpoznienie(czas, miejsca[i]);

                        // tu od timera odjac randomowy czas opoznienia 
                        Console.WriteLine("Czas transakcji update dla miejsca {0} wynosi: {1}", miejsca[i], czas.TotalMilliseconds);//timer.ElapsedMilliseconds.ToString());

                    }
                    daj_metode.dodaj_wynik(t_wynik, m_connection_update);
                }

                m_connection_update.Close();
            }


            m_connection.Close();
            m_connection_insert.Close();
            //m_connection_update.Close();
        }
    }

}


// JAK TO DZIALA W MS SQL
// https://edu.pjwstk.edu.pl/wiki/e2c3275d0e1a4bc0da360dd225d74a46/Default.aspx?Page=Poziomy%20izolacji%20w%20MS%20SQL%20Server&NS=&AspxAutoDetectCookieSupport=1
