-- =====================================================
-- CATÁLOGO DE PRODUCTOS - PAPELERÍA CASA CEJA
-- =====================================================
-- Usando categorías existentes: Papelería(1), Oficina(2), Escolar(3), Arte(4), Tecnología(5)
-- Usando unidades existentes: Pieza(1), Caja(2), Paquete(3)
-- =====================================================

-- 1. Primero crear la categoría "El Oso" para el ventilador especial
INSERT OR IGNORE INTO categories (id, name, discount, has_discount, active, created_at, updated_at) 
VALUES (6, 'El Oso', 10, 1, 1, datetime('now'), datetime('now'));

-- 2. Producto especial: Ventilador de Pedestal (código 3315)
INSERT INTO products (barcode, name, category_id, unit_id, presentation, iva, price_retail, price_wholesale, wholesale_quantity, price_special, price_dealer, active, created_at, updated_at, sync_status)
VALUES 
('3315', 'Ventilador de Pedestal', 6, 1, 'Pieza', 0, 1295, 1150, 3, 1095, 0, 1, datetime('now'), datetime('now'), 1);

-- 3. Productos de papelería
-- Categoría: Papelería (ID 1)
INSERT INTO products (barcode, name, category_id, unit_id, presentation, iva, price_retail, price_wholesale, wholesale_quantity, price_special, price_dealer, active, created_at, updated_at, sync_status)
VALUES 
('p001', 'Cuaderno Profesional 100 Hojas', 1, 1, 'Pieza', 0, 45, 38, 5, 0, 35, 1, datetime('now'), datetime('now'), 1),
('p002', 'Cuaderno Italiano 200 Hojas', 1, 1, 'Pieza', 0, 65, 55, 10, 58, 50, 1, datetime('now'), datetime('now'), 1),
('p003', 'Cuaderno Scribe 80 Hojas', 1, 1, 'Pieza', 0, 28, 24, 12, 0, 22, 1, datetime('now'), datetime('now'), 1),
('p004', 'Pluma BIC Cristal Azul', 1, 1, 'Pieza', 0, 8, 6, 12, 0, 5.5, 1, datetime('now'), datetime('now'), 1),
('p005', 'Pluma Gel Pilot G2 Negro', 1, 1, 'Pieza', 0, 18, 15, 10, 16, 14, 1, datetime('now'), datetime('now'), 1),
('p006', 'Lápiz Mirado No. 2', 1, 1, 'Pieza', 0, 5, 4, 24, 0, 3.5, 1, datetime('now'), datetime('now'), 1),
('p007', 'Portaminas Pentel 0.5mm', 1, 1, 'Pieza', 0, 25, 20, 6, 22, 18, 1, datetime('now'), datetime('now'), 1),
('p008', 'Plumón Sharpie Negro', 1, 1, 'Pieza', 0, 22, 18, 12, 20, 0, 1, datetime('now'), datetime('now'), 1),
('p009', 'Resistol Blanco 125g', 1, 1, 'Pieza', 0, 18, 15, 10, 0, 14, 1, datetime('now'), datetime('now'), 1),
('p010', 'Cinta Scotch 3M Grande', 1, 1, 'Pieza', 0, 35, 30, 6, 32, 28, 1, datetime('now'), datetime('now'), 1);

-- Categoría: Oficina (ID 2)
INSERT INTO products (barcode, name, category_id, unit_id, presentation, iva, price_retail, price_wholesale, wholesale_quantity, price_special, price_dealer, active, created_at, updated_at, sync_status)
VALUES 
('p011', 'Hojas Blancas Carta 500 Hojas', 2, 3, 'Paquete', 0, 185, 160, 3, 170, 0, 1, datetime('now'), datetime('now'), 1),
('p012', 'Engrapadora Metálica Mediana', 2, 1, 'Pieza', 0, 85, 72, 4, 0, 68, 1, datetime('now'), datetime('now'), 1),
('p013', 'Perforadora 2 Agujeros', 2, 1, 'Pieza', 0, 95, 80, 4, 88, 75, 1, datetime('now'), datetime('now'), 1),
('p014', 'Tijeras Escolares 5"', 2, 1, 'Pieza', 0, 32, 28, 6, 0, 26, 1, datetime('now'), datetime('now'), 1),
('p015', 'Corrector Líquido 20ml', 2, 1, 'Pieza', 0, 18, 15, 12, 16, 14, 1, datetime('now'), datetime('now'), 1),
('p016', 'Carpeta Tamaño Carta', 2, 1, 'Pieza', 0, 12, 10, 20, 0, 9, 1, datetime('now'), datetime('now'), 1),
('p017', 'Folder Manila Tamaño Oficio', 2, 1, 'Pieza', 0, 8, 6.5, 25, 0, 6, 1, datetime('now'), datetime('now'), 1),
('p018', 'Sobre Manila Oficio', 2, 1, 'Pieza', 0, 6, 5, 30, 0, 4.5, 1, datetime('now'), datetime('now'), 1),
('p019', 'Clip Estándar Caja 100 pzas', 2, 2, 'Caja', 0, 15, 12, 10, 0, 11, 1, datetime('now'), datetime('now'), 1),
('p020', 'Broches Baco No. 8 Caja 50 pzas', 2, 2, 'Caja', 0, 22, 18, 8, 20, 16, 1, datetime('now'), datetime('now'), 1);

-- Categoría: Escolar (ID 3)
INSERT INTO products (barcode, name, category_id, unit_id, presentation, iva, price_retail, price_wholesale, wholesale_quantity, price_special, price_dealer, active, created_at, updated_at, sync_status)
VALUES 
('p021', 'Mochila Escolar Básica', 3, 1, 'Pieza', 0, 295, 260, 2, 0, 250, 1, datetime('now'), datetime('now'), 1),
('p022', 'Estuche Escolar Triple', 3, 1, 'Pieza', 0, 85, 72, 4, 78, 68, 1, datetime('now'), datetime('now'), 1),
('p023', 'Compás Escolar Metálico', 3, 1, 'Pieza', 0, 45, 38, 6, 0, 36, 1, datetime('now'), datetime('now'), 1),
('p024', 'Transportador 180° Cristal', 3, 1, 'Pieza', 0, 12, 10, 12, 0, 9, 1, datetime('now'), datetime('now'), 1),
('p025', 'Juego Geométrico 4 Piezas', 3, 1, 'Juego', 0, 32, 28, 6, 30, 26, 1, datetime('now'), datetime('now'), 1),
('p026', 'Regla de Madera 30cm', 3, 1, 'Pieza', 0, 8, 6, 20, 0, 5.5, 1, datetime('now'), datetime('now'), 1),
('p027', 'Goma Blanca Pelikan', 3, 1, 'Pieza', 0, 5, 4, 24, 0, 3.5, 1, datetime('now'), datetime('now'), 1),
('p028', 'Sacapuntas Metálico Doble', 3, 1, 'Pieza', 0, 12, 10, 15, 0, 9, 1, datetime('now'), datetime('now'), 1),
('p029', 'Pegamento en Barra 21g', 3, 1, 'Pieza', 0, 15, 12, 12, 0, 11, 1, datetime('now'), datetime('now'), 1),
('p030', 'Marcatextos Amarillo', 3, 1, 'Pieza', 0, 12, 10, 12, 11, 9, 1, datetime('now'), datetime('now'), 1);

-- Categoría: Arte (ID 4)
INSERT INTO products (barcode, name, category_id, unit_id, presentation, iva, price_retail, price_wholesale, wholesale_quantity, price_special, price_dealer, active, created_at, updated_at, sync_status)
VALUES 
('p031', 'Colores Prismacolor Caja 12', 4, 2, 'Caja', 0, 185, 160, 3, 170, 0, 1, datetime('now'), datetime('now'), 1),
('p032', 'Acuarelas Pelikan 12 Colores', 4, 2, 'Caja', 0, 95, 82, 4, 88, 78, 1, datetime('now'), datetime('now'), 1),
('p033', 'Pincel Redondo No. 4', 4, 1, 'Pieza', 0, 35, 30, 6, 0, 28, 1, datetime('now'), datetime('now'), 1),
('p034', 'Cartulina Bristol Blanca', 4, 1, 'Pieza', 0, 8, 6.5, 20, 0, 6, 1, datetime('now'), datetime('now'), 1),
('p035', 'Papel Crepe 10 Colores', 4, 3, 'Paquete', 0, 48, 40, 5, 45, 38, 1, datetime('now'), datetime('now'), 1),
('p036', 'Plastilina 12 Colores', 4, 2, 'Caja', 0, 55, 48, 6, 50, 45, 1, datetime('now'), datetime('now'), 1),
('p037', 'Diamantina Surtida 6 Colores', 4, 3, 'Paquete', 0, 35, 30, 8, 32, 28, 1, datetime('now'), datetime('now'), 1),
('p038', 'Foami Tamaño Carta', 4, 1, 'Pieza', 0, 6, 5, 25, 0, 4.5, 1, datetime('now'), datetime('now'), 1),
('p039', 'Pintura Acrílica 60ml', 4, 1, 'Pieza', 0, 28, 24, 8, 26, 22, 1, datetime('now'), datetime('now'), 1),
('p040', 'Block de Dibujo 20 Hojas', 4, 1, 'Pieza', 0, 35, 30, 6, 32, 0, 1, datetime('now'), datetime('now'), 1);

-- Categoría: Tecnología (ID 5)
INSERT INTO products (barcode, name, category_id, unit_id, presentation, iva, price_retail, price_wholesale, wholesale_quantity, price_special, price_dealer, active, created_at, updated_at, sync_status)
VALUES 
('p041', 'USB 32GB Kingston', 5, 1, 'Pieza', 0, 185, 165, 3, 175, 0, 1, datetime('now'), datetime('now'), 1),
('p042', 'Mouse Inalámbrico Logitech', 5, 1, 'Pieza', 0, 295, 260, 2, 0, 250, 1, datetime('now'), datetime('now'), 1),
('p043', 'Teclado USB Básico', 5, 1, 'Pieza', 0, 185, 165, 3, 170, 160, 1, datetime('now'), datetime('now'), 1),
('p044', 'Cable USB Tipo C 1m', 5, 1, 'Pieza', 0, 65, 55, 5, 60, 0, 1, datetime('now'), datetime('now'), 1),
('p045', 'Audífonos In-Ear Básicos', 5, 1, 'Pieza', 0, 95, 82, 4, 88, 78, 1, datetime('now'), datetime('now'), 1),
('p046', 'Cargador USB Dual 2.1A', 5, 1, 'Pieza', 0, 125, 110, 3, 115, 105, 1, datetime('now'), datetime('now'), 1),
('p047', 'Hub USB 4 Puertos', 5, 1, 'Pieza', 0, 155, 135, 3, 145, 0, 1, datetime('now'), datetime('now'), 1),
('p048', 'Lector de Memorias SD/MicroSD', 5, 1, 'Pieza', 0, 75, 65, 4, 70, 60, 1, datetime('now'), datetime('now'), 1),
('p049', 'Cable HDMI 1.5m', 5, 1, 'Pieza', 0, 95, 82, 4, 0, 78, 1, datetime('now'), datetime('now'), 1),
('p050', 'Webcam HD 720p', 5, 1, 'Pieza', 0, 395, 350, 2, 370, 340, 1, datetime('now'), datetime('now'), 1);

-- =====================================================
-- RESUMEN DEL CATÁLOGO:
-- =====================================================
-- Total: 51 productos (50 de papelería/tecnología + 1 ventilador)
-- Categorías usadas: Papelería(1), Oficina(2), Escolar(3), Arte(4), Tecnología(5), El Oso(10)
-- Unidades usadas: Pieza(1), Caja(2), Paquete(3)
-- Producto especial 3315 (Ventilador) en categoría "El Oso"
-- =====================================================