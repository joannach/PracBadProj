using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjTransakcje
{
    class MetodyWyniki
    {
        public TimeSpan policzOpoznienie(TimeSpan czas, int opoznienie)
        {
            TimeSpan op_rand = TimeSpan.FromTicks(opoznienie);
            return czas - op_rand;
        }

        public void dodaj_wynik(Wynik w, SqlConnection m_connection)
        {
            string connectionString = "Data Source=DESKTOP-K6OJ71S;Initial Catalog=CINEMA;" + "Integrated Security=true";

            string queryString = "insert into Wynik(Konflikt, Opoznienie, StartT, StopT) values(@konflikt, @opoznienie, @start, @stop)";

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                // Create the Command and Parameter objects.
                SqlCommand command = new SqlCommand(queryString, m_connection);

                command.Parameters.AddWithValue("konflikt", w.w_konflikt);
                command.Parameters.AddWithValue("opoznienie", w.w_opoznienie);
                command.Parameters.AddWithValue("start", w.w_start);
                command.Parameters.AddWithValue("stop", w.w_stop);

                command.ExecuteNonQuery();
                Console.WriteLine("Dodano do tabeli wynikowej");
            }

        }
    }
}
