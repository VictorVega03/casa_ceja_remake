-- =====================================================
-- SCRIPT INICIAL - PAPELERÍA CASA CEJA
-- =====================================================
-- Orden de inserción:
--   1. Roles
--   2. Unidades de medida
--   3. Categorías
--   4. Sucursales
--   5. Usuarios
--   6. Productos
-- =====================================================
PRAGMA foreign_keys = OFF;
BEGIN TRANSACTION;

-- =====================================================
-- 1. ROLES
-- key debe coincidir con Constants.ROLE_ADMIN_KEY = "admin"
--                        Constants.ROLE_CASHIER_KEY = "cashier"
-- =====================================================
INSERT OR IGNORE INTO roles (id, name, key, access_level, active, created_at, updated_at)
VALUES
    (1, 'Administrador', 'admin',   100, 1, datetime('now'), datetime('now')),
    (2, 'Cajero',        'cashier',  10, 1, datetime('now'), datetime('now'));


-- =====================================================
-- 2. UNIDADES DE MEDIDA
-- =====================================================
INSERT OR IGNORE INTO units (id, name, active, created_at, updated_at, sync_status)
VALUES
    (1, 'Pieza',     1, datetime('now'), datetime('now'), 0),
    (2, 'Caja',      1, datetime('now'), datetime('now'), 0),
    (3, 'Paquete',   1, datetime('now'), datetime('now'), 0),
    (4, 'Metro',     1, datetime('now'), datetime('now'), 0),
    (5, 'Kilogramo', 1, datetime('now'), datetime('now'), 0),
    (6, 'Litro',     1, datetime('now'), datetime('now'), 0);


-- =====================================================
-- 3. CATEGORÍAS
-- =====================================================
INSERT OR IGNORE INTO categories (id, name, discount, has_discount, active, created_at, updated_at, sync_status)
VALUES
    (1, 'Papelería',  0,  0, 1, datetime('now'), datetime('now'), 0),
    (2, 'Oficina',    0,  0, 1, datetime('now'), datetime('now'), 0),
    (3, 'Escolar',    0,  0, 1, datetime('now'), datetime('now'), 0),
    (4, 'Arte',       0,  0, 1, datetime('now'), datetime('now'), 0),
    (5, 'Tecnología', 0,  0, 1, datetime('now'), datetime('now'), 0),
    (6, 'El Oso',     10, 1, 1, datetime('now'), datetime('now'), 0);


-- =====================================================
-- 4. SUCURSALES
-- =====================================================
INSERT OR IGNORE INTO branches (id, name, address, email, razon_social, active, created_at, updated_at, sync_status)
VALUES
    (1, 'Casa Ceja - Carranza', 'Direccion Carranza', 'sucursal@casaceja.com', 'Casa Ceja S.A de C.V', 1, datetime('now'), datetime('now'), 0),
    (2, 'Casa Ceja - Obregon',  'Direccion Obregon',  'obregon@casaceja.com',  'Casa Ceja Obregon',    1, datetime('now'), datetime('now'), 0);


-- =====================================================
-- 5. USUARIOS
-- branch_id se deja NULL (se asigna desde la app al iniciar sesión)
-- user_type: 1=Administrador, 2=Cajero
-- =====================================================
INSERT OR IGNORE INTO users (id, name, email, username, password, user_type, branch_id, active, created_at, updated_at, sync_status)
VALUES
    (1, 'Administrador', 'admin@casaceja.com',  'admin', 'admin', 1, NULL, 1, datetime('now'), datetime('now'), 0),
    (2, 'Cajero',        'cajero@casaceja.com', 'qwe',   'qwe',   2, NULL, 1, datetime('now'), datetime('now'), 0);
   



-- =====================================================
-- 6. PRODUCTOS
-- wholesale_quantity = 5 para todos (mayoreo activo con 5+ unidades)
-- price_special > 0  → precio especial
-- price_dealer  > 0  → precio distribuidor
-- =====================================================

-- Producto especial: Ventilador de Pedestal (categoría El Oso)
INSERT OR IGNORE INTO products (barcode, name, category_id, unit_id, presentation, iva, price_retail, price_wholesale, wholesale_quantity, price_special, price_dealer, active, created_at, updated_at, sync_status)
VALUES
    ('3315', 'Ventilador de Pedestal', 6, 1, 'Pieza', 0, 1295, 1150, 5, 1095, 0, 1, datetime('now'), datetime('now'), 0);

-- Categoría: Papelería (ID 1) — con precio especial
INSERT OR IGNORE INTO products (barcode, name, category_id, unit_id, presentation, iva, price_retail, price_wholesale, wholesale_quantity, price_special, price_dealer, active, created_at, updated_at, sync_status)
VALUES
    ('p001', 'Cuaderno Profesional 100 Hojas', 1, 1, 'Pieza', 0, 45,  38,  5, 40,  0,   1, datetime('now'), datetime('now'), 0),
    ('p002', 'Cuaderno Italiano 200 Hojas',    1, 1, 'Pieza', 0, 65,  55,  5, 58,  0,   1, datetime('now'), datetime('now'), 0),
    ('p003', 'Cuaderno Scribe 80 Hojas',       1, 1, 'Pieza', 0, 28,  24,  5, 25,  0,   1, datetime('now'), datetime('now'), 0),
    ('p004', 'Pluma BIC Cristal Azul',         1, 1, 'Pieza', 0, 8,   6,   5, 7,   0,   1, datetime('now'), datetime('now'), 0),
    ('p005', 'Pluma Gel Pilot G2 Negro',       1, 1, 'Pieza', 0, 18,  15,  5, 16,  0,   1, datetime('now'), datetime('now'), 0),
    ('p006', 'Lápiz Mirado No. 2',             1, 1, 'Pieza', 0, 5,   4,   5, 4.5, 0,   1, datetime('now'), datetime('now'), 0),
    ('p007', 'Portaminas Pentel 0.5mm',        1, 1, 'Pieza', 0, 25,  20,  5, 22,  0,   1, datetime('now'), datetime('now'), 0),
    ('p008', 'Plumón Sharpie Negro',           1, 1, 'Pieza', 0, 22,  18,  5, 20,  0,   1, datetime('now'), datetime('now'), 0),
    ('p009', 'Resistol Blanco 125g',           1, 1, 'Pieza', 0, 18,  15,  5, 16,  0,   1, datetime('now'), datetime('now'), 0),
    ('p010', 'Cinta Scotch 3M Grande',         1, 1, 'Pieza', 0, 35,  30,  5, 32,  0,   1, datetime('now'), datetime('now'), 0);

-- Categoría: Oficina (ID 2) — con precio distribuidor
INSERT OR IGNORE INTO products (barcode, name, category_id, unit_id, presentation, iva, price_retail, price_wholesale, wholesale_quantity, price_special, price_dealer, active, created_at, updated_at, sync_status)
VALUES
    ('p011', 'Hojas Blancas Carta 500 Hojas',   2, 3, 'Paquete', 0, 185, 160, 5, 0, 155, 1, datetime('now'), datetime('now'), 0),
    ('p012', 'Engrapadora Metálica Mediana',     2, 1, 'Pieza',   0, 85,  72,  5, 0, 68,  1, datetime('now'), datetime('now'), 0),
    ('p013', 'Perforadora 2 Agujeros',           2, 1, 'Pieza',   0, 95,  80,  5, 0, 75,  1, datetime('now'), datetime('now'), 0),
    ('p014', 'Tijeras Escolares 5"',             2, 1, 'Pieza',   0, 32,  28,  5, 0, 26,  1, datetime('now'), datetime('now'), 0),
    ('p015', 'Corrector Líquido 20ml',           2, 1, 'Pieza',   0, 18,  15,  5, 0, 14,  1, datetime('now'), datetime('now'), 0),
    ('p016', 'Carpeta Tamaño Carta',             2, 1, 'Pieza',   0, 12,  10,  5, 0, 9,   1, datetime('now'), datetime('now'), 0),
    ('p017', 'Folder Manila Tamaño Oficio',      2, 1, 'Pieza',   0, 8,   6.5, 5, 0, 6,   1, datetime('now'), datetime('now'), 0),
    ('p018', 'Sobre Manila Oficio',              2, 1, 'Pieza',   0, 6,   5,   5, 0, 4.5, 1, datetime('now'), datetime('now'), 0),
    ('p019', 'Clip Estándar Caja 100 pzas',      2, 2, 'Caja',    0, 15,  12,  5, 0, 11,  1, datetime('now'), datetime('now'), 0),
    ('p020', 'Broches Baco No. 8 Caja 50 pzas',  2, 2, 'Caja',    0, 22,  18,  5, 0, 16,  1, datetime('now'), datetime('now'), 0);

-- Categoría: Escolar (ID 3)
INSERT OR IGNORE INTO products (barcode, name, category_id, unit_id, presentation, iva, price_retail, price_wholesale, wholesale_quantity, price_special, price_dealer, active, created_at, updated_at, sync_status)
VALUES
    ('p021', 'Mochila Escolar Básica',     3, 1, 'Pieza', 0, 295, 260, 5, 0,  250, 1, datetime('now'), datetime('now'), 0),
    ('p022', 'Estuche Escolar Triple',     3, 1, 'Pieza', 0, 85,  72,  5, 78, 68,  1, datetime('now'), datetime('now'), 0),
    ('p023', 'Compás Escolar Metálico',    3, 1, 'Pieza', 0, 45,  38,  5, 0,  36,  1, datetime('now'), datetime('now'), 0),
    ('p024', 'Transportador 180° Cristal', 3, 1, 'Pieza', 0, 12,  10,  5, 0,  9,   1, datetime('now'), datetime('now'), 0),
    ('p025', 'Juego Geométrico 4 Piezas',  3, 1, 'Juego', 0, 32,  28,  5, 30, 26,  1, datetime('now'), datetime('now'), 0),
    ('p026', 'Regla de Madera 30cm',       3, 1, 'Pieza', 0, 8,   6,   5, 0,  5.5, 1, datetime('now'), datetime('now'), 0),
    ('p027', 'Goma Blanca Pelikan',        3, 1, 'Pieza', 0, 5,   4,   5, 0,  3.5, 1, datetime('now'), datetime('now'), 0),
    ('p028', 'Sacapuntas Metálico Doble',  3, 1, 'Pieza', 0, 12,  10,  5, 0,  9,   1, datetime('now'), datetime('now'), 0),
    ('p029', 'Pegamento en Barra 21g',     3, 1, 'Pieza', 0, 15,  12,  5, 0,  11,  1, datetime('now'), datetime('now'), 0),
    ('p030', 'Marcatextos Amarillo',       3, 1, 'Pieza', 0, 12,  10,  5, 11, 9,   1, datetime('now'), datetime('now'), 0);

-- Categoría: Arte (ID 4)
INSERT OR IGNORE INTO products (barcode, name, category_id, unit_id, presentation, iva, price_retail, price_wholesale, wholesale_quantity, price_special, price_dealer, active, created_at, updated_at, sync_status)
VALUES
    ('p031', 'Colores Prismacolor Caja 12',  4, 2, 'Caja',    0, 185, 160, 5, 170, 0,   1, datetime('now'), datetime('now'), 0),
    ('p032', 'Acuarelas Pelikan 12 Colores', 4, 2, 'Caja',    0, 95,  82,  5, 88,  78,  1, datetime('now'), datetime('now'), 0),
    ('p033', 'Pincel Redondo No. 4',         4, 1, 'Pieza',   0, 35,  30,  5, 0,   28,  1, datetime('now'), datetime('now'), 0),
    ('p034', 'Cartulina Bristol Blanca',     4, 1, 'Pieza',   0, 8,   6.5, 5, 0,   6,   1, datetime('now'), datetime('now'), 0),
    ('p035', 'Papel Crepe 10 Colores',       4, 3, 'Paquete', 0, 48,  40,  5, 45,  38,  1, datetime('now'), datetime('now'), 0),
    ('p036', 'Plastilina 12 Colores',        4, 2, 'Caja',    0, 55,  48,  5, 50,  45,  1, datetime('now'), datetime('now'), 0),
    ('p037', 'Diamantina Surtida 6 Colores', 4, 3, 'Paquete', 0, 35,  30,  5, 32,  28,  1, datetime('now'), datetime('now'), 0),
    ('p038', 'Foami Tamaño Carta',           4, 1, 'Pieza',   0, 6,   5,   5, 0,   4.5, 1, datetime('now'), datetime('now'), 0),
    ('p039', 'Pintura Acrílica 60ml',        4, 1, 'Pieza',   0, 28,  24,  5, 26,  22,  1, datetime('now'), datetime('now'), 0),
    ('p040', 'Block de Dibujo 20 Hojas',     4, 1, 'Pieza',   0, 35,  30,  5, 32,  0,   1, datetime('now'), datetime('now'), 0);

-- Categoría: Tecnología (ID 5)
INSERT OR IGNORE INTO products (barcode, name, category_id, unit_id, presentation, iva, price_retail, price_wholesale, wholesale_quantity, price_special, price_dealer, active, created_at, updated_at, sync_status)
VALUES
    ('p041', 'USB 32GB Kingston',             5, 1, 'Pieza', 0, 185, 165, 5, 175, 0,   1, datetime('now'), datetime('now'), 0),
    ('p042', 'Mouse Inalámbrico Logitech',    5, 1, 'Pieza', 0, 295, 260, 5, 0,   250, 1, datetime('now'), datetime('now'), 0),
    ('p043', 'Teclado USB Básico',            5, 1, 'Pieza', 0, 185, 165, 5, 170, 160, 1, datetime('now'), datetime('now'), 0),
    ('p044', 'Cable USB Tipo C 1m',           5, 1, 'Pieza', 0, 65,  55,  5, 60,  0,   1, datetime('now'), datetime('now'), 0),
    ('p045', 'Audífonos In-Ear Básicos',      5, 1, 'Pieza', 0, 95,  82,  5, 88,  78,  1, datetime('now'), datetime('now'), 0),
    ('p046', 'Cargador USB Dual 2.1A',        5, 1, 'Pieza', 0, 125, 110, 5, 115, 105, 1, datetime('now'), datetime('now'), 0),
    ('p047', 'Hub USB 4 Puertos',             5, 1, 'Pieza', 0, 155, 135, 5, 145, 0,   1, datetime('now'), datetime('now'), 0),
    ('p048', 'Lector de Memorias SD/MicroSD', 5, 1, 'Pieza', 0, 75,  65,  5, 70,  60,  1, datetime('now'), datetime('now'), 0),
    ('p049', 'Cable HDMI 1.5m',               5, 1, 'Pieza', 0, 95,  82,  5, 0,   78,  1, datetime('now'), datetime('now'), 0),
    ('p050', 'Webcam HD 720p',                5, 1, 'Pieza', 0, 395, 350, 5, 370, 340, 1, datetime('now'), datetime('now'), 0);

COMMIT;

-- =====================================================
-- RESUMEN:
-- Roles:      2  (Administrador key='admin', Cajero key='cashier')
-- Unidades:   6  (Pieza, Caja, Paquete, Metro, Kilogramo, Litro)
-- Categorías: 6  (Papelería, Oficina, Escolar, Arte, Tecnología, El Oso)
-- Sucursales: 2  (Carranza id=1, Obregon id=2)
-- Usuarios:   2  (admin/admin → admin, qwe/qwe → cajero)
-- Productos:  51 (50 catálogo + 1 ventilador)
-- =====================================================
