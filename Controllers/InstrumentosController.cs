using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using System.Text.RegularExpressions;
using TodoMVC.Data;
using TodoMVC.Models;

namespace TodoMVC.Controllers;

public class InstrumentosController : Controller
{
    public IActionResult Index()
    {
        return View(ObtenerInstrumentos());
    }

    public IActionResult Crear()
    {
        return View();
    }

    [HttpPost]
    public IActionResult Crear(Instrumento instrumento)
    {
        if (!TituloValido(instrumento.Titulo))
        {
            ViewBag.Error = "El nombre del instrumento es obligatorio y debe tener mas de 2 caracteres.";
            return View(instrumento);
        }

        var tituloLimpio = instrumento.Titulo!.Trim();

        using var conexion = Database.AbrirConexion();
        var sql = "INSERT INTO Instrumentos (Titulo, Completada, Cantidad) VALUES (@titulo, 0, 1)";
        using var comando = new SqliteCommand(sql, conexion);
        comando.Parameters.AddWithValue("@titulo", tituloLimpio);
        comando.ExecuteNonQuery();

        return RedirectToAction("Index");
    }

    public IActionResult Completar(int id)
    {
        using var conexion = Database.AbrirConexion();
        var sql = "UPDATE Instrumentos SET Completada = 1 WHERE Id = @id";
        using var comando = new SqliteCommand(sql, conexion);
        comando.Parameters.AddWithValue("@id", id);
        comando.ExecuteNonQuery();

        return RedirectToAction("Index");
    }

    public IActionResult CambiarCantidad(int id, int cambio)
    {
        if (cambio != 1 && cambio != -1)
            return RedirectToAction("Index");

        using var conexion = Database.AbrirConexion();
        var sql = @"
            UPDATE Instrumentos
            SET Cantidad = CASE
                WHEN Cantidad + @cambio < 1 THEN 1
                ELSE Cantidad + @cambio
            END
            WHERE Id = @id AND Completada = 0";
        using var comando = new SqliteCommand(sql, conexion);
        comando.Parameters.AddWithValue("@id", id);
        comando.Parameters.AddWithValue("@cambio", cambio);
        comando.ExecuteNonQuery();

        return RedirectToAction("Index");
    }
    public IActionResult Eliminar(int id)
    {
        using var conexion = Database.AbrirConexion();
        var sql = "DELETE FROM Instrumentos WHERE Id = @id";
        using var comando = new SqliteCommand(sql, conexion);
        comando.Parameters.AddWithValue("@id", id);
        comando.ExecuteNonQuery();

        return RedirectToAction("Index");
    }

    [HttpPost]
    public IActionResult UpdateTitulo(int id, string titulo)
    {
        if (!TituloValido(titulo))
        {
            ViewBag.Error = "El nuevo nombre es obligatorio y debe tener mas de 2 caracteres.";
            return View("Index", ObtenerInstrumentos());
        }

        using var conexion = Database.AbrirConexion();
        var sql = "UPDATE Instrumentos SET Titulo = @titulo WHERE Id = @id";
        using var comando = new SqliteCommand(sql, conexion);
        comando.Parameters.AddWithValue("@titulo", titulo.Trim());
        comando.Parameters.AddWithValue("@id", id);
        comando.ExecuteNonQuery();

        return RedirectToAction("Index");
    }

    private static bool TituloValido(string? titulo)
    {
        return !string.IsNullOrWhiteSpace(titulo) && Regex.IsMatch(titulo.Trim(), @"^.{3,}$");
    }

    private static List<Instrumento> ObtenerInstrumentos()
    {
        var instrumentos = new List<Instrumento>();

        using var conexion = Database.AbrirConexion();
        var sql = "SELECT Id, Titulo, Completada, Cantidad FROM Instrumentos ORDER BY Id DESC";
        using var comando = new SqliteCommand(sql, conexion);
        using var reader = comando.ExecuteReader();

        while (reader.Read())
        {
            instrumentos.Add(new Instrumento
            {
                Id = reader.GetInt32(0),
                Titulo = reader.GetString(1),
                Completada = reader.GetInt32(2) == 1,
                Cantidad = reader.GetInt32(3)
            });
        }

        return instrumentos;
    }
}



/*

PARTE 1 - CAMBIO DE CONTEXTO REALIZADO
Contexto elegido: Carrito de instrumentos.
Entidad: Instrumento (Titulo, Completada: bool).
El valor Completada se usa como confirmacion de que el instrumento ya esta
agregado/confirmado dentro del carrito, manteniendo la logica base del CRUD.

PUNTO DE PARTIDA
────────────────
Este proyecto sigue la estructura clásica
de tres capas (Modelo / Vista / Controlador) y realiza las operaciones CRUD
basicas: listar, crear, completar y eliminar instrumentos.
 
Vuestra misión es entender ese código base, cambiar el contexto de la
aplicación y añadir las mejoras que se describen a continuación.
 
 

PARTE 1 · CAMBIO DE CONTEXTO  (obligatorio)


 
  A) Biblioteca personal
     Entidad: Libro  (titulo, autor, leido: bool)
 
  B) Gestor de películas / series
     Entidad: Pelicula  (titulo, genero, vista: bool)
 
  C) Lista de la compra
     Entidad: Producto  (nombre, cantidad, comprado: bool)
 
  D) Agenda de contactos
     Entidad: Contacto  (nombre, telefono, email)
 
  E) Registro de entrenamientos
     Entidad: Entreno  (fecha, tipo, duracion_minutos, completado: bool)
 
La elección es libre, pero hay que justificarla en la memoria (ver Parte 4).
 
 

PARTE 2 · MEJORAS OBLIGATORIAS

 
2.1  AÑADIR UN CAMPO EXTRA AL MODELO
     El modelo original solo tiene Id, Titulo y Completada.
     Añade al menos un campo adicional relevante para tu contexto.
     Ejemplos: fecha de creación, prioridad (alta/media/baja), categoría...
 
     Pasos que implica:
     · Actualizar la clase del modelo (Models/)
     · Modificar el CREATE TABLE en Database.cs
     · Actualizar las queries SQL en el controlador
     · Mostrar el nuevo campo en la vista Index
 
2.2  EDITAR UN REGISTRO
     La aplicación base no permite editar una entrada ya creada.
     Implementa la funcionalidad de edición:
 
     El formulario de edición debe pre-rellenar los campos con los valores
     actuales del registro.
 
2.3  VALIDACIÓN EN EL CONTROLADOR
     Antes de insertar o actualizar, comprueba que los campos obligatorios
     no están vacíos. Si la validación falla, devuelve el formulario con un
     mensaje de error visible al usuario (sin redirigir).
 
     Mínimo: validar que el campo principal (nombre, título…) no está vacío
     y que tiene más de 2 caracteres. (puedes utilizar regex en el fromulario)
 
 
PARTE 3 · MEJORA OPCIONAL (puntuación extra)

 
Implementa UNA de las siguientes mejoras adicionales:
 
  · FILTRADO: Añade un filtro en la vista Index (pendientes / completados / todos)
              pasando el filtro como parámetro en la URL (?filtro=pendientes).

  · DETALLE:    Añade una vista de detalle (GET /Entidad/Detalle/{id}) que
                muestre toda la información de un registro en una página propia.
 
 

PARTE 4 · DOCUMENTACIÓN

Genera un archivo de readme con la siguiente información:
 
  1. Contexto elegido y justificación (3-5 líneas)
  2. Diagrama o descripción de la estructura de la BD (tabla, columnas, tipos)
  3. URLs de la aplicación y qué acción realiza cada una
  4. Explicación de los cambios realizados respecto al código base
  5. Capturas de pantalla de todas la vistas funcionando y estiladas con CSS en plan guapo.
  

  */
