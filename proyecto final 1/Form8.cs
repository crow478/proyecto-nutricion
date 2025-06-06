using System;
using System.Collections.Generic;
using System.Data; // Necesario para usar DataTable
using System.Data.SqlClient;
using System.Text;
using System.Windows.Forms;

namespace proyecto_final_1
{
    public partial class Form8 : Form
    {
        private string connectionString = "Data Source=LAPTOP-R1VO187T\\SQLEXPRESS;Initial Catalog=Proyecto;Integrated Security=True";
        private double carbohidratosConsumidos;
        private double proteinasConsumidas;
        private double grasasConsumidas;

        public Form8()
        {
            InitializeComponent();
        }

        private void Form8_Load(object sender, EventArgs e)
        {
            // Cargar los usuarios (id y nombre completo) en el ComboBox
            // ¡IMPORTANTE! Aquí concatenamos las columnas 'nombres' y 'apellidos'
            // para mostrar el nombre completo en el ComboBox.
            // Asegúrate de que los nombres de las columnas 'nombres' y 'apellidos' en tu DB sean exactos.
            string queryUsuarios = "SELECT idUsuario, nombres + ' ' + apellidos AS NombreCompleto FROM paciente";

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                try
                {
                    connection.Open();
                    SqlCommand command = new SqlCommand(queryUsuarios, connection);
                    SqlDataReader reader = command.ExecuteReader();

                    // Creamos un DataTable para almacenar los datos.
                    // Esto permite que el ComboBox mapee DisplayMember y ValueMember.
                    DataTable dt = new DataTable();
                    dt.Load(reader); // Carga los datos del SqlDataReader en el DataTable

                    // Limpiamos los ítems existentes (si los hubiera)
                    comboBoxUsuarios.Items.Clear();

                    // Asignamos el DataTable como la fuente de datos del ComboBox
                    comboBoxUsuarios.DataSource = dt;

                    // Especificamos qué columna del DataTable se mostrará al usuario (el nombre completo)
                    comboBoxUsuarios.DisplayMember = "NombreCompleto";

                    // Especificamos qué columna del DataTable se usará como valor interno (el ID)
                    comboBoxUsuarios.ValueMember = "idUsuario";

                    // Habilitamos AutoComplete en el ComboBox para facilitar la búsqueda
                    comboBoxUsuarios.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
                    comboBoxUsuarios.AutoCompleteSource = AutoCompleteSource.ListItems;

                    reader.Close();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error al cargar los usuarios: " + ex.Message);
                }
            }
        }

        // Este es el evento para el botón que realiza los cálculos y muestra el análisis nutricional.
        // Lo he dejado con el nombre original 'button1_Click' que tenías en tu primer archivo.
        private void button1_Click(object sender, EventArgs e)
        {
            // Verificamos si hay un usuario seleccionado.
            // Usamos SelectedValue porque es donde está el idUsuario.
            if (comboBoxUsuarios.SelectedValue == null)
            {
                MessageBox.Show("Por favor, seleccione un usuario.");
                return;
            }

            // Obtenemos el idUsuario del SelectedValue del ComboBox.
            // Es importante convertirlo al tipo de dato correcto (en este caso, string para la consulta SQL).
            string idUsuarioSeleccionado = comboBoxUsuarios.SelectedValue.ToString();

            // Consultas para obtener datos del paciente, actividad física y productos consumidos
            string queryPaciente = @"
SELECT p.peso, p.altura, pa.edad, pa.genero
FROM registro_paciente p
JOIN paciente pa ON pa.idUsuario = p.idUsuario
WHERE pa.idUsuario = @idUsuario";

            string queryActividad = @"
SELECT SUM(tiempoSemanal) as tiempoSemanal
FROM actividad_fisica
WHERE idUsuario = @idUsuario";

            string queryProductosConsumidos = @"
SELECT
    ISNULL(p.caloriaProducto, 0) AS caloriaProducto,
    ISNULL(p.carbohidratoProducto, 0) AS carbohidratoProducto,
    ISNULL(p.proteinaProducto, 0) AS proteinaProducto,
    ISNULL(p.grasasProducto, 0) AS grasasProducto,
    ISNULL(ap.cantidad_consumo, 1) AS cantidad
FROM producto p
INNER JOIN alimentacion_paciente ap ON ap.idProducto = p.idProducto
WHERE ap.idUsuario = @idUsuario";

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                try
                {
                    connection.Open();

                    // Obtener datos del paciente
                    SqlCommand command = new SqlCommand(queryPaciente, connection);
                    command.Parameters.AddWithValue("@idUsuario", idUsuarioSeleccionado);
                    SqlDataReader reader = command.ExecuteReader();

                    if (reader.Read())
                    {
                        double pesoOriginal = reader["peso"] != DBNull.Value ? Convert.ToDouble(reader["peso"]) : 0;
                        double alturaOriginal = reader["altura"] != DBNull.Value ? Convert.ToDouble(reader["altura"]) : 0;
                        int edad = reader["edad"] != DBNull.Value ? Convert.ToInt32(reader["edad"]) : 0;
                        string genero = reader["genero"] != DBNull.Value ? reader["genero"].ToString() : "";
                        reader.Close();

                        if (pesoOriginal == 0 || alturaOriginal == 0 || edad == 0 || string.IsNullOrEmpty(genero))
                        {
                            MessageBox.Show("Faltan datos del paciente (peso, altura, edad o género).");
                            return;
                        }

                        // Obtener tiempo de actividad física
                        SqlCommand commandActividad = new SqlCommand(queryActividad, connection);
                        commandActividad.Parameters.AddWithValue("@idUsuario", idUsuarioSeleccionado);

                        object resultadoActividad = commandActividad.ExecuteScalar();
                        int tiempoSemanal = 0;

                        if (resultadoActividad != null && resultadoActividad != DBNull.Value)
                        {
                            tiempoSemanal = Convert.ToInt32(resultadoActividad);
                        }

                        if (tiempoSemanal <= 0)
                        {
                            MessageBox.Show("No se encontró actividad física registrada para este usuario.");
                            return;
                        }

                        // Cálculos de IMC y TMB
                        double pesoKg = pesoOriginal * 0.453592;
                        double alturaCm = alturaOriginal;
                        double alturaM = alturaCm / 100.0;
                        double imc = pesoKg / (alturaM * alturaM);

                        textBoxPeso.Text = pesoKg.ToString("F2");
                        textBoxAltura.Text = alturaCm.ToString("F2");
                        textBoxIMC.Text = imc.ToString("F2");

                        double tmb;
                        if (genero.ToLower() == "masculino")
                            tmb = 88.362 + (13.397 * pesoKg) + (4.799 * alturaCm) - (5.677 * edad);
                        else
                            tmb = 447.593 + (9.247 * pesoKg) + (3.098 * alturaCm) - (4.330 * edad);

                        double factorActividad;
                        if (tiempoSemanal < 2)
                            factorActividad = 1.2;
                        else if (tiempoSemanal <= 5)
                            factorActividad = 1.55;
                        else if (tiempoSemanal <= 10)
                            factorActividad = 1.725;
                        else
                            factorActividad = 1.9;

                        double requerimientoCalorico = tmb * factorActividad;
                        textBoxRequerimientoCalorico.Text = requerimientoCalorico.ToString("F2");

                        // Cálculo de calorías y macronutrientes consumidos
                        double caloriasConsumidasTotales = 0;
                        double carbohidratosConsumidos = 0;
                        double proteinasConsumidas = 0;
                        double grasasConsumidas = 0;

                        SqlCommand commandProductos = new SqlCommand(queryProductosConsumidos, connection);
                        commandProductos.Parameters.AddWithValue("@idUsuario", idUsuarioSeleccionado);
                        SqlDataReader readerProductos = commandProductos.ExecuteReader();

                        while (readerProductos.Read())
                        {
                            int carbohidratos = Convert.ToInt32(readerProductos["carbohidratoProducto"]);
                            int proteinas = Convert.ToInt32(readerProductos["proteinaProducto"]);
                            int grasas = Convert.ToInt32(readerProductos["grasasProducto"]);
                            int cantidad = Convert.ToInt32(readerProductos["cantidad"]);

                            caloriasConsumidasTotales += ((carbohidratos * 4) + (proteinas * 4) + (grasas * 9)) * cantidad;
                            carbohidratosConsumidos += carbohidratos * cantidad;
                            proteinasConsumidas += proteinas * cantidad;
                            grasasConsumidas += grasas * cantidad;
                        }

                        readerProductos.Close();
                        textBoxCaloriasConsumidas.Text = caloriasConsumidasTotales.ToString("F2");

                        // Recomendación de macronutrientes por porcentaje
                        double caloriasPorCarbohidratos = requerimientoCalorico * 0.55;
                        double caloriasPorProteinas = requerimientoCalorico * 0.15;
                        double caloriasPorGrasas = requerimientoCalorico * 0.30;

                        double gramosCarbohidratosNecesarios = caloriasPorCarbohidratos / 4;
                        double gramosProteinasNecesarios = caloriasPorProteinas / 4;
                        double gramosGrasasNecesarios = caloriasPorGrasas / 9;

                        StringBuilder alerta = new StringBuilder();

                        // Mensajes de alerta de macronutrientes
                        if (carbohidratosConsumidos > gramosCarbohidratosNecesarios)
                            alerta.AppendLine("Exceso de carbohidratos. Considera reducir su consumo.");
                        else if (carbohidratosConsumidos < gramosCarbohidratosNecesarios)
                            alerta.AppendLine("Déficit de carbohidratos. Asegúrate de consumir suficientes carbohidratos.");

                        if (proteinasConsumidas > gramosProteinasNecesarios)
                            alerta.AppendLine("Exceso de proteínas. Considera reducir el consumo de proteínas.");
                        else if (proteinasConsumidas < gramosProteinasNecesarios)
                            alerta.AppendLine("Déficit de proteínas. Asegúrate de consumir suficientes proteínas.");

                        if (grasasConsumidas > gramosGrasasNecesarios)
                            alerta.AppendLine("Exceso de grasas. Considera reducir las grasas en tu dieta.");
                        else if (grasasConsumidas < gramosGrasasNecesarios)
                            alerta.AppendLine("Déficit de grasas. Asegúrate de consumir grasas saludables.");

                        // Clasificación del IMC
                        string clasificacionIMC = "";
                        if (imc < 18.5)
                            clasificacionIMC = "IMC indica bajo peso. Se recomienda aumentar la ingesta calórica y consultar con un nutricionista.";
                        else if (imc >= 18.5 && imc <= 24.9)
                            clasificacionIMC = "IMC dentro del rango normal. Mantén una alimentación balanceada y actividad física regular.";
                        else if (imc >= 25 && imc <= 29.9)
                            clasificacionIMC = "IMC indica sobrepeso. Se recomienda mejorar la calidad de la dieta y aumentar la actividad física.";
                        else if (imc >= 30)
                            clasificacionIMC = "IMC indica obesidad. Es importante buscar orientación médica y nutricional para prevenir complicaciones.";

                        alerta.Insert(0, clasificacionIMC + Environment.NewLine + Environment.NewLine);

                        // ----- INICIO DE GENERACION DE RECOMENDACIONES DE DIETA -----

                        // Alimentos comunes y su aporte aproximado en gramos de macronutrientes por porción
                        var alimentosCarbohidratos = new Dictionary<string, double>()
                        {
                            {"Arroz cocido (1 taza)", 45}, // gramos carbohidrato por porción
                            {"Pan integral (1 rebanada)", 15},
                            {"Papa hervida (1 mediana)", 30},
                            {"Frijoles cocidos (1/2 taza)", 20}
                        };

                        var alimentosProteinas = new Dictionary<string, double>()
                        {
                            {"Pechuga de pollo (100g)", 31},
                            {"Huevos (1 unidad)", 6},
                            {"Queso bajo en grasa (30g)", 7},
                            {"Lentejas cocidas (1/2 taza)", 9}
                        };

                        var alimentosGrasas = new Dictionary<string, double>()
                        {
                            {"Aceite de oliva (1 cucharada)", 14},
                            {"Aguacate (1/2 unidad mediana)", 15},
                            {"Nueces (30g)", 20},
                            {"Semillas de chía (1 cucharada)", 5}
                        };

                        // Función para distribuir gramos en porciones aproximadas
                        string GenerarRecomendacionAlimentos(Dictionary<string, double> alimentos, double gramosNecesarios)
                        {
                            StringBuilder sb = new StringBuilder();
                            double gramosRestantes = gramosNecesarios;

                            foreach (var alimento in alimentos)
                            {
                                if (gramosRestantes <= 0)
                                    break;

                                double porciones = Math.Round(gramosRestantes / alimento.Value, 1);
                                if (porciones > 0)
                                {
                                    sb.AppendLine($"{porciones} porciones de {alimento.Key}");
                                    gramosRestantes -= porciones * alimento.Value;
                                }
                            }

                            if (gramosRestantes > 0)
                            {
                                sb.AppendLine($"Cantidad adicional de alimentos para completar {gramosRestantes:F2} gramos.");
                            }

                            return sb.ToString();
                        }

                        string recomendacionCarbohidratos = "Carbohidratos recomendados:\n" + GenerarRecomendacionAlimentos(alimentosCarbohidratos, gramosCarbohidratosNecesarios);
                        string recomendacionProteinas = "Proteínas recomendadas:\n" + GenerarRecomendacionAlimentos(alimentosProteinas, gramosProteinasNecesarios);
                        string recomendacionGrasas = "Grasas recomendadas:\n" + GenerarRecomendacionAlimentos(alimentosGrasas, gramosGrasasNecesarios);

                        alerta.AppendLine("\n=== Recomendaciones básicas de dieta ===\n");
                        alerta.AppendLine(recomendacionCarbohidratos);
                        alerta.AppendLine(recomendacionProteinas);
                        alerta.AppendLine(recomendacionGrasas);

                        // ----- FIN DE GENERACION DE RECOMENDACIONES -----

                        textBoxAnalisisNutricional.Text = alerta.ToString();
                    }
                    else
                    {
                        MessageBox.Show("No se encontraron datos para el usuario seleccionado.");
                    }

                    reader.Close();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error al calcular el IMC, requerimiento calórico y calorías consumidas: " + ex.Message);
                }
            }
        }

        // Este es el evento para el botón que muestra el Form10 (asumo que es otro formulario).
        private void button2_Click(object sender, EventArgs e)
        {
            Form10 formulario10 = new Form10();
            formulario10.Show();
            this.Hide();
        }

        // Este es el evento para el botón que guarda los datos en la base de datos.
        // ¡IMPORTANTE! Si este es tu botón "Guardar", deberías considerar renombrarlo
        // en el diseñador de Visual Studio para evitar confusión con el otro button1_Click.
        private void button1_Click_1(object sender, EventArgs e)
        {
            // Validamos que se haya seleccionado un usuario.
            if (comboBoxUsuarios.SelectedValue == null)
            {
                MessageBox.Show("Por favor, seleccione un usuario antes de guardar.");
                return;
            }

            // Recuperamos el idUsuario del SelectedValue del ComboBox.
            // Es crucial que se convierta a 'int' si la columna idUsuario en 'calculospaciente' es INT.
            int idUsuario = Convert.ToInt32(comboBoxUsuarios.SelectedValue);

            // Verificamos si los campos numéricos tienen datos válidos antes de intentar parsear.
            if (string.IsNullOrWhiteSpace(textBoxIMC.Text) ||
                string.IsNullOrWhiteSpace(textBoxCaloriasConsumidas.Text) ||
                string.IsNullOrWhiteSpace(textBoxRequerimientoCalorico.Text))
            {
                MessageBox.Show("Por favor, calcule los datos primero (haga clic en el botón de cálculo).");
                return;
            }

            // Recuperar los datos de los TextBox
            double masaCorporal = double.Parse(textBoxIMC.Text);
            double caloriasDiarias = double.Parse(textBoxCaloriasConsumidas.Text);
            double caloriasRecomendadas = double.Parse(textBoxRequerimientoCalorico.Text);
            string recomendaciones = textBoxAnalisisNutricional.Text;

            // Llamar a la función para insertar los datos en la base de datos
            InsertarDatos(idUsuario, (int)masaCorporal, (int)caloriasDiarias, (int)caloriasRecomendadas, recomendaciones);
        }

        // Función para insertar los datos en la base de datos
        private void InsertarDatos(int idUsuario, int masaCorporal, int caloriasDiarias, int caloriasRecomendadas, string recomendaciones)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                // Crear la consulta SQL para insertar los datos
                string query = "INSERT INTO calculospaciente (idUsuario, Masa_corporal, CaloriasDiarias, CaloriasRecomendadas, Recomendaciones) " +
                               "VALUES (@idUsuario, @masaCorporal, @caloriasDiarias, @caloriasRecomendadas, @recomendaciones)";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    // Agregar los parámetros de la consulta
                    command.Parameters.AddWithValue("@idUsuario", idUsuario);
                    command.Parameters.AddWithValue("@masaCorporal", masaCorporal);
                    command.Parameters.AddWithValue("@caloriasDiarias", caloriasDiarias);
                    command.Parameters.AddWithValue("@caloriasRecomendadas", caloriasRecomendadas);
                    command.Parameters.AddWithValue("@recomendaciones", recomendaciones);

                    // Abrir la conexión y ejecutar la consulta
                    try
                    {
                        connection.Open();
                        command.ExecuteNonQuery();
                        MessageBox.Show("Datos guardados exitosamente.");
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Error al guardar los datos: " + ex.Message);
                    }
                }
            }
        }

        // Evento que se dispara cuando cambia la selección en el ComboBox.
        // Actualmente está vacío, pero podrías agregar lógica si la necesitas.
        private void comboBoxUsuarios_SelectedIndexChanged(object sender, EventArgs e)
        {
            // Si quisieras que los datos se cargaran automáticamente al seleccionar un usuario,
            // podrías llamar a button1_Click(null, EventArgs.Empty); aquí.
            // Ten cuidado de evitar bucles infinitos si la lógica de carga es compleja.
        }

        // Evento para el botón que regresa al menú principal.
        private void button4_Click(object sender, EventArgs e)
        {
            menu menu1 = new menu();
            menu1.Show();
            this.Hide();
        }

        private void button2_Click_1(object sender, EventArgs e)
        {

            Form10 menu1 = new Form10();
            menu1.Show();
            this.Hide();
        }
    }
}