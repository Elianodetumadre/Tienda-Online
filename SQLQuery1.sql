USE DBCRUDUsuarios;
GO

-- Tabla CarritoItems 
CREATE TABLE CarritoItems ( 
    Id INT IDENTITY(1,1) PRIMARY KEY, 
    UsuarioId INT NOT NULL, 
    ProductoId INT NOT NULL, 
    Cantidad INT NOT NULL CHECK (Cantidad > 0), 
    FechaAgregado DATETIME DEFAULT GETDATE(), 
    FOREIGN KEY (UsuarioId) REFERENCES Usuarios(Id) ON DELETE CASCADE, 
    FOREIGN KEY (ProductoId) REFERENCES Productos(Id) ON DELETE CASCADE 
); 
GO

-- Tabla Pedidos 
CREATE TABLE Pedidos ( 
    Id INT IDENTITY(1,1) PRIMARY KEY, 
    UsuarioId INT NOT NULL, 
    FechaPedido DATETIME DEFAULT GETDATE(), 
    Total DECIMAL(18,2) NOT NULL, 
    Estado INT NOT NULL DEFAULT 0, -- 0=Pendiente, 1=Confirmado, 2=Enviado, 3=Entregado, 4=Cancelado 
    DireccionEnvio NVARCHAR(200) NULL, 
    MetodoPago NVARCHAR(50) NULL, 
    NumeroReferencia NVARCHAR(20) NULL, 
    FOREIGN KEY (UsuarioId) REFERENCES Usuarios(Id) ON DELETE CASCADE 
); 
GO

-- Tabla PedidoDetalles 
CREATE TABLE PedidoDetalles ( 
    Id INT IDENTITY(1,1) PRIMARY KEY, 
    PedidoId INT NOT NULL, 
    ProductoId INT NOT NULL, 
    Cantidad INT NOT NULL, 
    PrecioUnitario DECIMAL(18,2) NOT NULL, 
    FOREIGN KEY (PedidoId) REFERENCES Pedidos(Id) ON DELETE CASCADE, 
    FOREIGN KEY (ProductoId) REFERENCES Productos(Id) ON DELETE CASCADE 
);
GO