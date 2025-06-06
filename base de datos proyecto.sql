create database Proyecto
use Proyecto 

CREATE TABLE paciente  (
    idUsuario INT IDENTITY(1,1) PRIMARY KEY,
    nombres VARCHAR(50),
    apellidos VARCHAR(50),
    edad INT,
    genero VARCHAR(20),
    municipio VARCHAR(50),
    departamento VARCHAR(50)
);

CREATE TABLE Registro_Paciente (
    idRegistroPaciente INT IDENTITY(1,1) PRIMARY KEY,
    idUsuario INT,
    fecha_registro DATE,
    peso FLOAT,
    altura FLOAT,
    FOREIGN KEY (idUsuario) REFERENCES paciente (idUsuario)
);

CREATE TABLE Actividad_Fisica (
    idActividadFisica INT IDENTITY(1,1) PRIMARY KEY,
    idRegistroPaciente INT,
    idUsuario INT,
    tipoActividad VARCHAR(50),
    tiempoSemanal INT,
    FOREIGN KEY (idRegistroPaciente) REFERENCES Registro_Paciente(idRegistroPaciente),
    FOREIGN KEY (idUsuario) REFERENCES paciente (idUsuario)
);

CREATE TABLE Informacion_Nutricional (
    idInformacionNutricional INT IDENTITY(1,1) PRIMARY KEY,
    descripcionNutricional VARCHAR(255),
    dietaNutricional VARCHAR(400),
    tiempoDieta INT
);

CREATE TABLE Producto (
    idProducto INT IDENTITY(1,1) PRIMARY KEY,
    idUsuario INT,
    tipoproducto VARCHAR(50),
    nombreProducto VARCHAR(50),
    caloriaProducto INT,
    carbohidratoProducto INT,
    proteinaProducto INT,
    grasasProducto INT,
    energiaProducto INT,
    FOREIGN KEY (idUsuario) REFERENCES paciente (idUsuario)
);

CREATE TABLE UnidadMedida (
    idUnidadMedida INT IDENTITY(1,1) PRIMARY KEY,
    idUsuario INT,
    nombreUnidadMedida VARCHAR(50),
    FOREIGN KEY (idUsuario) REFERENCES paciente (idUsuario)
);

CREATE TABLE Alimentacion_Paciente (
    idAlimentacion INT IDENTITY(1,1) PRIMARY KEY,
    idUsuario INT,
    fecha_alimentacion DATE,
    idProducto INT,
    cantidad_consumo INT,
    idUnidadMedida INT,
    FOREIGN KEY (idUsuario) REFERENCES paciente (idUsuario),
    FOREIGN KEY (idProducto) REFERENCES Producto(idProducto),
    FOREIGN KEY (idUnidadMedida) REFERENCES UnidadMedida(idUnidadMedida)
);

CREATE TABLE CalculosPaciente (
    idUsuario INT,
    Masa_corporal INT,
    CaloriasDiarias INT,
    CaloriasRecomendadas INT,
    Recomendaciones VARCHAR(250),
    FOREIGN KEY (idUsuario) REFERENCES paciente (idUsuario)
);
