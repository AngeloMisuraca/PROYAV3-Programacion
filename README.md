# 🎵 Mi Carrito de Instrumentos
---

## Tabla de contenidos

- [1. Contexto y justificación](#1-contexto-y-justificación)
- [2. Estructura de la base de datos](#2-estructura-de-la-base-de-datos)
- [3. URLs y acciones](#3-urls-y-acciones)
- [4. Cambios realizados respecto al código base](#4-cambios-realizados-respecto-al-código-base)
- [5. Capturas de pantalla](#5-capturas-de-pantalla)
- [Ejecución](#ejecución)

---

## 1. Contexto y justificación

Como aficionado a la música y a la audiofilia, decidí crear una herramienta personal para gestionar la renovación de mi equipo de sonido. En lugar de usar una lista genérica, diseñé una aplicación específica que permite:

- **Listar instrumentos o equipos**: Mantén un registro centralizado de todos los artículos que necesitas adquirir o actualizar en tu carrito de compra, desde micrófonos hasta interfaces de audio.
- **Indicar cantidad de unidades**: Especifica cuántas unidades necesitas de cada ítem sin duplicar entradas. Esto es especialmente útil cuando requieres múltiples unidades del mismo producto.
- **Marcar como confirmados**: Indica cuándo un equipo ya ha sido agregado al carrito de compra real, facilitando el seguimiento de qué está pendiente vs. qué ya está listo.
- **Editar rápidamente los nombres**: Modifica el nombre de cualquier instrumento directamente desde la lista principal sin necesidad de ir a otra página, ideal para correcciones rápidas.

El objetivo principal es proporcionar una vista clara, manejable y funcional de qué necesitas comprar, permitiéndote priorizar compras de manera flexible y eficiente. Esta herramienta simplifica la planificación de inversiones en equipo de audio, que suele ser un proceso largo y complicado.

---

## 2. Estructura de la base de datos

La aplicación utiliza **SQLite** como sistema de gestión de base de datos, lo que proporciona una solución ligera, sin necesidad de servidor externo. La base de datos se almacena localmente en el archivo `instrumentos.db` y contiene la tabla principal `Instrumentos`:

### Tabla: Instrumentos

| Columna | Tipo | Descripción |
|---------|------|-------------|
| `Id` | INTEGER PRIMARY KEY | Identificador único, generado automáticamente por la BD (autoincremental) |
| `Titulo` | TEXT NOT NULL | Nombre del instrumento o equipo (obligatorio, no puede estar vacío) |
| `Completada` | INTEGER (0/1) | Estado binario: 0 = pendiente de compra, 1 = confirmado/agregado al carrito |
| `Cantidad` | INTEGER | Número de unidades necesarias de este ítem (mínimo permitido es 1) |

**Notas técnicas:**
- La columna `Id` se genera automáticamente mediante autoincremento, asegurando que cada registro tenga un identificador único e inmutable.
- `Titulo` es obligatorio y no puede contener solo espacios en blanco; se valida tanto en el controlador como en la interfaz.
- `Completada` se almacena como entero (0 o 1) siguiendo la convención de SQLite de no tener booleano nativo; en C# se mapea a `bool`.
- `Cantidad` nunca debe ser 0 o negativa, ya que una cantidad de 0 implica que el ítem no existe lógicamente.

### Inicialización de la Base de Datos

La base de datos se inicializa automáticamente cuando la aplicación se ejecuta por primera vez mediante el método `Database.Inicializar()`:
- Si la tabla `Instrumentos` no existe, se crea con todas las columnas descritas arriba.
- Si la tabla ya existe (de una versión anterior del proyecto), se realiza una verificación de esquema.
- Si falta la columna `Cantidad` en una tabla existente, se ejecuta un `ALTER TABLE` para añadirla sin perder datos previos.

Este enfoque garantiza compatibilidad hacia atrás y facilita actualizaciones sin pérdida de información.

---

## 3. URLs y acciones

La aplicación expone los siguientes endpoints HTTP a través del controlador `InstrumentosController`. Todos los endpoints están dentro de la ruta base `/Instrumentos`:

| Método | URL | Acción | Descripción |
|--------|-----|--------|-------------|
| GET | `/Instrumentos` | Listar instrumentos | Obtiene la lista completa de instrumentos de la BD y la renderiza en la vista `Index`. Los instrumentos se ordenan normalmente sin filtro especial. |
| GET | `/Instrumentos/Crear` | Formulario de creación | Devuelve el formulario HTML para que el usuario ingrese un nuevo instrumento. Este formulario no tiene datos precargados. |
| POST | `/Instrumentos/Crear` | Crear instrumento | Recibe los datos del formulario (título y cantidad), valida que cumplan los requisitos, inserta el nuevo instrumento en la BD, y redirige a la lista. |
| GET | `/Instrumentos/Completar/{id}` | Alternar estado | Cambia el estado del instrumento con ID especificado: si está pendiente pasa a confirmado, si está confirmado vuelve a pendiente. Útil para marcar un ítem como agregado al carrito o deshacer esa acción. |
| GET | `/Instrumentos/CambiarCantidad/{id}?cambio=1` | Incrementar cantidad | Suma 1 a la cantidad del instrumento (solo si está pendiente). Usualmente usado con un botón "+" en la interfaz. |
| GET | `/Instrumentos/CambiarCantidad/{id}?cambio=-1` | Decrementar cantidad | Resta 1 a la cantidad del instrumento (solo si está pendiente y la cantidad es mayor a 1). Usualmente usado con un botón "−" en la interfaz. |
| GET | `/Instrumentos/Eliminar/{id}` | Eliminar instrumento | Borra completamente el instrumento de la BD. Esta acción es irreversible. Se puede usar para remover ítems que ya no se necesitan. |
| POST | `/Instrumentos/UpdateTitulo/{id}` | Editar título inline | Recibe un nuevo título para el instrumento con ID especificado, lo valida, lo actualiza en la BD, y devuelve la vista actualizada. Permite editar desde la lista sin navegar a otra página. |

**Notas de seguridad y comportamiento:**
- Los endpoints con cambios en cantidad (`CambiarCantidad`) solo funcionan en instrumentos pendientes. No se puede cambiar la cantidad de ítems ya confirmados.
- El mínimo de cantidad es 1; intentar decrementar cuando la cantidad es 1 no hace nada.
- Todos los campos de texto (título) se validan para evitar espacios vacíos o entradas muy cortas.

---

## 4. Cambios realizados respecto al código base

Esta sección detalla exhaustivamente las modificaciones implementadas desde la estructura base del proyecto Todo original. El proyecto partió de una aplicación de tareas genérica y se transformó en una herramienta especializada para gestionar un carrito de instrumentos.

### 4.1 Campo adicional: Cantidad

**Propósito:** Permitir múltiples unidades del mismo ítem sin duplicar registros en la BD.

**Implementación técnica:**
- Se agregó la propiedad `Cantidad` (tipo `int`) al modelo `Instrumento` en `Models/Instrumento.cs`.
- En la BD, la columna `Cantidad INTEGER` se inicializa con valor por defecto de 1 para garantizar integridad.
- En las vistas, se renderiza la cantidad junto al nombre del instrumento y se acepta en el formulario de creación.

**Ventajas:**
- Evita duplicación de filas en la BD cuando necesitas múltiples unidades del mismo producto.
- Ejemplo: En lugar de 3 entradas "Micrófono Shure SM7B", tienes 1 entrada con `Cantidad = 3`, lo que ahorra espacio y complejidad.
- Simplifica las consultas SQL que trabajan con instrumentos: una sola fila por producto.

**Restricciones implementadas:**
- Cantidad mínima es 1 (no se permite 0 o valores negativos).
- La cantidad solo se puede cambiar si el instrumento está en estado "pendiente" (`Completada = 0`).
- Si intentas establecer cantidad a 0, se rechaza en la validación del controlador.

### 4.2 Edición inline del título

**Propósito:** Permitir renombrar un instrumento directamente desde la lista sin navegar a una página separada.

**Implementación técnica:**
- En la vista `Views/Instrumentos/Index.cshtml`, cada instrumento tiene un pequeño formulario oculto que se muestra mediante JavaScript al hacer click en un botón "Editar".
- El formulario POST envía el nuevo título al endpoint `POST /Instrumentos/UpdateTitulo/{id}`.
- El controlador valida el nuevo título y actualiza la BD si es correcto.
- La vista se redirecciona a la lista principal, mostrando los cambios inmediatamente.

**Flujo de usuario:**
1. Usuario ve la lista de instrumentos.
2. Hace click en el botón "Editar" junto a un instrumento.
3. Un campo de texto aparece (inline) con el nombre actual.
4. Usuario cambia el nombre y presiona Enter o click en "Guardar".
5. El sistema actualiza la BD y recarga la página con el nuevo nombre.

**Ventajas:**
- Mejor experiencia de usuario: menos clics y navegación.
- Contexto siempre visible: no pierdes de vista el resto de instrumentos mientras editas uno.
- Ideal para correcciones rápidas y cambios sobre la marcha.

### 4.3 Validación en el controlador

**Reglas de validación implementadas:**
- **Título no vacío:** Se rechaza cualquier título que sea null, una cadena vacía o solo espacios en blanco.
- **Longitud mínima:** El título debe tener más de 2 caracteres (se valida con regex).
- **Cantidad válida:** Si se envía cantidad, debe ser un entero mayor a 0.

**Manejo de errores:**
- Si la validación falla, el controlador devuelve la vista `Crear` con un `ModelState` que contiene el mensaje de error.
- Razor renderiza estos errores en HTML usando `Html.ValidationMessageFor()`.
- El usuario ve el formulario nuevamente con el mensaje de error visible y los datos parcialmente preservados (si es posible).
- **Importante:** No se realiza redirección en caso de error, el usuario sigue en el mismo sitio.

**Ejemplo de validación en código (pseudocódigo):**
```csharp
 private static bool TituloValido(string? titulo)
    {
        return !string.IsNullOrWhiteSpace(titulo) && Regex.IsMatch(titulo.Trim(), @"^.{3,}$");
    }
```

### 4.4 Gestión inteligente de cantidad con restricciones

**Endpoint: `CambiarCantidad/{id}?cambio=±1`**

Este endpoint es crucial para la interacción del usuario y tiene dos reglas fundamentales:

1. **Cantidad mínima es 1:** No se permite cantidad 0 o negativa.
   - Si intentas decrementar cuando `Cantidad = 1`, la operación no ocurre.
   - Esta regla previene inconsistencias lógicas (un ítem con cantidad 0 no tiene sentido).

2. **Solo para instrumentos pendientes:** No se puede cambiar la cantidad de ítems ya confirmados (`Completada = 1`).
   - Esto protege contra cambios accidentales en ítems que ya están en el carrito real.
   - Si un usuario necesita cambiar la cantidad después de confirmar, debe primero "desconfirmar" el ítem.

**Flujo técnico:**
- Usuario hace click en botón "+" o "−" en la vista.
- Se envía GET a `/Instrumentos/CambiarCantidad/{id}?cambio=1` (o `-1`).
- El controlador:
  1. Obtiene el instrumento por ID de la BD.
  2. Verifica que `Completada == 0` (está pendiente).
  3. Calcula `NuevaCantidad = cantidad_actual + cambio`.
  4. Verifica que `NuevaCantidad >= 1`.
  5. Si todas las verificaciones pasan, actualiza la BD.
  6. Redirecciona a la lista principal.

**Ventajas de estas restricciones:**
- Previene datos inconsistentes o sin sentido lógico.
- Mejora la experiencia de usuario al evitar estados problemáticos.
- Proporciona protección contra acciones accidentales.

### 4.5 Interfaz mejorada y optimizada para usabilidad

**Cambios en la vista `Index.cshtml`:**

La vista ahora muestra información mucho más rica y acciones inmediatas:

- **Indicador visual del estado:** Cada instrumento muestra claramente si es "Pendiente" o "Confirmado" (puede ser un símbolo 🔲/✅ o color diferente).
- **Cantidad junto al nombre:** El usuario ve de un vistazo cuántas unidades necesita de cada ítem (ej: "Micrófono (×3)").
- **Botones de control inmediatos:**
  - "+" : Incrementa cantidad en 1.
  - "−" : Decrementa cantidad en 1.
  - "✓" o "Confirmar" : Marca como agregado al carrito.
  - "Editar" : Abre el formulario inline para cambiar el nombre.
  - "Eliminar" : Borra el instrumento de la lista.
- **Formulario inline para renombrar:** Como se menciona en la sección 4.2.
- **Organización visual:** Todos los elementos están estructurados para que las tareas más comunes sean accesibles de un vistazo, sin necesidad de scroll excesivo en listas pequeñas.

**Responsividad:**
- La interfaz está diseñada para funcionar en dispositivos de diferentes tamaños.
- Los botones son lo suficientemente grandes para ser clickeables en pantallas táctiles.
- El layout se adapta manteniendo la claridad de la información.

### 4.6 Migración segura de datos y evolución del esquema

**Problema inicial:** Si usuarios tenían una base de datos antigua sin la columna `Cantidad`, al actualizar la aplicación se rompería.

**Solución implementada en `Data/Database.cs`:**

```csharp
public static void Inicializar()
{
    using (var conexion = new SqliteConnection(_connectionString))
    {
        conexion.Open();

        // Crear tabla si no existe
        string createTableSQL = @"
            CREATE TABLE IF NOT EXISTS Instrumentos (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Titulo TEXT NOT NULL,
                Completada INTEGER DEFAULT 0,
                Cantidad INTEGER DEFAULT 1
            )";
        
        using (var command = new SqliteCommand(createTableSQL, conexion))
        {
            command.ExecuteNonQuery();
        }

        // Verificar y añadir columna Cantidad si no existe (migración)
        string checkColumnSQL = @"
            PRAGMA table_info(Instrumentos)";
        
        using (var command = new SqliteCommand(checkColumnSQL, conexion))
        {
            using (var reader = command.ExecuteReader())
            {
                bool cantidadExists = false;
                while (reader.Read())
                {
                    if (reader["name"].ToString() == "Cantidad")
                    {
                        cantidadExists = true;
                        break;
                    }
                }

                if (!cantidadExists)
                {
                    string addColumnSQL = @"
                        ALTER TABLE Instrumentos 
                        ADD COLUMN Cantidad INTEGER DEFAULT 1";
                    
                    using (var addCommand = new SqliteCommand(addColumnSQL, conexion))
                    {
                        addCommand.ExecuteNonQuery();
                    }
                }
            }
        }
    }
}
```

**¿Cómo funciona?**
1. Intenta crear la tabla `Instrumentos` con todas las columnas (si no existe, perfecto; si existe, `IF NOT EXISTS` lo previene).
2. Usa `PRAGMA table_info()` para obtener la estructura actual de la tabla.
3. Verifica si la columna `Cantidad` existe.
4. Si no existe, ejecuta `ALTER TABLE` para añadirla con valor por defecto 1.
5. Los datos anteriores no se pierden; solo se añade la nueva columna con un valor sensato.

**Ventajas:**
- Compatibilidad hacia atrás garantizada.
- Usuarios con BDs antiguas pueden actualizar sin perder datos.
- Transiciones de versión suaves y sin intervención manual.

### 4.7 Separación clara de responsabilidades

**Estructura del proyecto:**
```
Controllers/
  └─ InstrumentosController.cs    (Lógica de negocio, validación, control de flujo)
Data/
  └─ Database.cs                  (Acceso a BD, inicialización, migraciones)
Models/
  └─ Instrumento.cs               (Definición de entidad, propiedades)
Views/Instrumentos/
  ├─ Index.cshtml                 (Listar y manipular instrumentos)
  └─ Crear.cshtml                 (Formulario para crear nuevo instrumento)
```

- **Modelo (`Instrumento.cs`):** Solo define propiedades y validaciones de datos.
- **Datos (`Database.cs`):** Maneja toda la comunicación con SQLite (consultas, migraciones).
- **Controlador (`InstrumentosController.cs`):** Orquesta la lógica de negocio, valida entrada, coordina modelo y datos.
- **Vistas:** Renderización HTML, interacción con usuario, envío de formularios.

Esta separación facilita mantener el código, realizar cambios sin efectos colaterales, y hacer pruebas más fáciles.

---

## 5. Capturas de pantalla

Aquí irán las capturas que muestran la interfaz funcionando:

- **Vista principal (Index):** Muestra la lista completa de instrumentos con estado, cantidad, y botones de control.
- **Vista de creación:** Formulario para agregar un nuevo instrumento con campos de título y cantidad.
- **Edición inline:** Demostración de cómo editar un título directamente desde la lista sin navegar.

---

## Ejecución

Para ejecutar la aplicación en tu máquina:

### Requisitos previos
- **.NET 8 o posterior** instalado en tu sistema
- **Git** (opcional, para clonar el repositorio)

### Pasos para ejecutar

1. **Abre una terminal** en la carpeta raíz del proyecto

```powershell
cd c:\Users\angel\Desktop\PROYECTOS_VSCODE\PROGRAMACION\TODO
```

2. **Restaura las dependencias de NuGet** (primer ejecutar):

```powershell
dotnet restore
```

3. **Ejecuta la aplicación**:

```powershell
dotnet run
```

4. **Accede a la aplicación** desde tu navegador:
   - Normalmente en `http://localhost:5000` o `http://localhost:5001`
   - La consola indicará el puerto exacto si es diferente

5. **Navega a la funcionalidad principal**:
   - Dirígete a `/Instrumentos` en tu navegador
   - Verás la lista de instrumentos y podrás empezar a crear, editar, confirmar y eliminar ítems

### Modo de desarrollo (con auto-recargar)

Si deseas que la aplicación se recompile automáticamente al hacer cambios en el código:

```powershell
dotnet watch run
```

### Detener la aplicación

Presiona `Ctrl+C` en la terminal donde corre la aplicación.

---

