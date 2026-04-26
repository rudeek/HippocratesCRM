CREATE TABLE manufacturer (
    manufacturer_id SERIAL PRIMARY KEY,
    name            VARCHAR(150) NOT NULL,
    country         VARCHAR(100) NOT NULL,
    address         VARCHAR(255) NOT NULL,
    phone           VARCHAR(20)  NOT NULL,
    email           VARCHAR(150) NOT NULL
);

CREATE TABLE product (
    product_id            SERIAL PRIMARY KEY,
    name                  VARCHAR(150)  NOT NULL,
    manufacturer_id       INT           NOT NULL REFERENCES manufacturer(manufacturer_id) ON DELETE CASCADE,
    expiration_date       DATE          NOT NULL,
    production_date       DATE          NOT NULL,
    unit                  VARCHAR(50)   DEFAULT 'шт',
    description           TEXT,
    prescription_required BOOLEAN       NOT NULL DEFAULT FALSE,
    purchase_price        NUMERIC(10,2) NOT NULL CHECK(purchase_price > 0),
    sale_price            NUMERIC(10,2) NOT NULL CHECK(sale_price > 0)
);

CREATE TABLE pharmacy (
    pharmacy_id   SERIAL PRIMARY KEY,
    address       VARCHAR(255) NOT NULL,
    phone         VARCHAR(20)  NOT NULL,
    working_hours VARCHAR(100) NOT NULL
);

CREATE TABLE employee (
    employee_id SERIAL PRIMARY KEY,
    full_name   VARCHAR(150)  NOT NULL,
    idnp        VARCHAR(13)   UNIQUE NOT NULL,
    phone       VARCHAR(20)   NOT NULL,
    address     VARCHAR(255)  NOT NULL,
    salary      NUMERIC(10,2) NOT NULL,
    position    VARCHAR(100)  NOT NULL
);

CREATE TABLE receipt (
    receipt_id     SERIAL PRIMARY KEY,
    receipt_number INT           NOT NULL UNIQUE,
    pharmacy_id    INT           NOT NULL REFERENCES pharmacy(pharmacy_id) ON DELETE CASCADE,
    employee_id    INT           NOT NULL REFERENCES employee(employee_id) ON DELETE CASCADE,
    total_amount   NUMERIC(10,2) NOT NULL DEFAULT 0,
    date           DATE          NOT NULL DEFAULT CURRENT_DATE,
    time           TIME          NOT NULL DEFAULT CURRENT_TIME
);

CREATE TABLE order_item (
    receipt_id  INT           NOT NULL REFERENCES receipt(receipt_id) ON DELETE CASCADE,
    product_id  INT           NOT NULL REFERENCES product(product_id) ON DELETE CASCADE,
    quantity    INT           NOT NULL CHECK (quantity > 0),
    unit_price  NUMERIC(10,2) NOT NULL,
    total_price NUMERIC(10,2) GENERATED ALWAYS AS (quantity * unit_price * (1 - discount / 100.0)) STORED,
    discount    NUMERIC(5,2)  NOT NULL DEFAULT 0 CHECK (discount >= 0 AND discount <= 100),
    PRIMARY KEY (receipt_id, product_id)
);

CREATE TABLE stock_balance (
    pharmacy_id   INT NOT NULL REFERENCES pharmacy(pharmacy_id) ON DELETE CASCADE,
    product_id    INT NOT NULL REFERENCES product(product_id) ON DELETE CASCADE,
    remaining_qty INT NOT NULL DEFAULT 0 CHECK (remaining_qty >= 0),
    PRIMARY KEY (pharmacy_id, product_id)
);

CREATE OR REPLACE FUNCTION set_unit_price()
RETURNS TRIGGER AS $$
BEGIN
    NEW.unit_price := (SELECT sale_price FROM product WHERE product_id = NEW.product_id);
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER trg_set_unit_price
BEFORE INSERT ON order_item
FOR EACH ROW
EXECUTE FUNCTION set_unit_price();


INSERT INTO manufacturer (name, country, address, phone, email) VALUES
('Bayer AG',             'Germany',     'Leverkusen, Kaiser-Wilhelm-Allee 1', '+4921430000',   'contact@bayer.com'),
('Pfizer Inc.',          'USA',         'New York, 235 E 42nd St',            '+12125732323',  'info@pfizer.com'),
('Novartis AG',          'Switzerland', 'Basel, Lichtstrasse 35',             '+41613241111',  'info@novartis.com'),
('Roche Holding AG',     'Switzerland', 'Basel, Grenzacherstrasse 124',       '+41616881111',  'contact@roche.com'),
('Sanofi S.A.',          'France',      'Paris, 54 Rue La Boetie',            '+33153773000',  'info@sanofi.com'),
('GlaxoSmithKline',      'UK',          'London, 980 Great West Rd',          '+442089908000', 'info@gsk.com'),
('AstraZeneca',          'UK',          'Cambridge, 1 Francis Crick Ave',     '+441223837000', 'contact@astrazeneca.com'),
('Johnson & Johnson',    'USA',         'New Jersey, One J&J Plaza',          '+17325242455',  'info@jnj.com'),
('Merck & Co.',          'USA',         'New Jersey, 2000 Galloping Hill Rd', '+19087404000',  'info@merck.com'),
('Abbott Laboratories',  'USA',         'Illinois, 100 Abbott Park Rd',       '+18474377000',  'info@abbott.com'),
('Teva Pharmaceuticals', 'Israel',      'Tel Aviv, 5 Basel St',               '+97236267000',  'info@teva.com'),
('Actavis Group',        'Iceland',     'Reykjavik, Reykjavikurvegi 76',      '+3545503000',   'info@actavis.com'),
('KRKA d.d.',            'Slovenia',    'Novo Mesto, Smarjeska cesta 6',      '+38673311111',  'info@krka.si'),
('Gedeon Richter',       'Hungary',     'Budapest, Gyomroi ut 19-21',         '+3614314000',   'info@richter.hu'),
('Stada Arzneimittel',   'Germany',     'Bad Vilbel, Stadastrasse 2-18',      '+4961019500',   'info@stada.de'),
('Sandoz AG',            'Switzerland', 'Basel, Lichtstrasse 35',             '+41613246111',  'info@sandoz.com'),
('Recordati S.p.A.',     'Italy',       'Milan, Via Civitali 1',              '+390248787111', 'info@recordati.it'),
('Servier',              'France',      'Suresnes, 50 Rue Carnot',            '+33155726000',  'info@servier.com'),
('Boehringer Ingelheim', 'Germany',     'Ingelheim, Binger Strasse 173',      '+496132770',    'info@boehringer.com'),
('Zentiva Group',        'Czech Rep.',  'Prague, U kabelovny 130',            '+420261091111', 'info@zentiva.com');

INSERT INTO product (name, manufacturer_id, expiration_date, production_date, unit, description, prescription_required, purchase_price, sale_price) VALUES
('Aspirin 500mg',       1,  '2026-06-01', '2024-06-01', 'шт', 'Pain reliever and fever reducer',        FALSE,  5.00,  8.50),
('Amoxicillin 500mg',   2,  '2026-03-15', '2024-03-15', 'шт', 'Antibiotic for bacterial infections',    TRUE,  12.00, 18.00),
('Ibuprofen 400mg',     3,  '2026-09-20', '2024-09-20', 'шт', 'Anti-inflammatory painkiller',           FALSE,  6.50, 10.00),
('Metformin 850mg',     4,  '2025-12-01', '2023-12-01', 'шт', 'Diabetes type 2 treatment',              TRUE,   8.00, 13.50),
('Lisinopril 10mg',     5,  '2026-07-10', '2024-07-10', 'шт', 'ACE inhibitor for blood pressure',       TRUE,   9.00, 15.00),
('Atorvastatin 20mg',   6,  '2026-11-05', '2024-11-05', 'шт', 'Cholesterol-lowering statin',            TRUE,  14.00, 22.00),
('Omeprazole 20mg',     7,  '2026-04-22', '2024-04-22', 'шт', 'Proton pump inhibitor for acid reflux',  FALSE,  7.50, 12.00),
('Paracetamol 500mg',   8,  '2026-08-30', '2024-08-30', 'шт', 'Analgesic and antipyretic',              FALSE,  3.50,  6.00),
('Losartan 50mg',       9,  '2026-02-14', '2024-02-14', 'шт', 'Angiotensin receptor blocker',           TRUE,  10.50, 17.00),
('Cetirizine 10mg',     10, '2027-01-01', '2025-01-01', 'шт', 'Antihistamine for allergies',            FALSE,  5.50,  9.00),
('Doxycycline 100mg',   11, '2026-05-18', '2024-05-18', 'шт', 'Broad-spectrum antibiotic',              TRUE,  11.00, 16.50),
('Simvastatin 40mg',    12, '2026-10-09', '2024-10-09', 'шт', 'Statin for cholesterol reduction',       TRUE,  13.00, 20.00),
('Clopidogrel 75mg',    13, '2026-12-25', '2024-12-25', 'шт', 'Antiplatelet agent',                     TRUE,  16.00, 25.00),
('Pantoprazole 40mg',   14, '2026-06-30', '2024-06-30', 'шт', 'Proton pump inhibitor',                  FALSE,  8.50, 14.00),
('Amlodipine 5mg',      15, '2026-03-03', '2024-03-03', 'шт', 'Calcium channel blocker',                TRUE,   9.50, 15.50),
('Warfarin 5mg',        16, '2025-11-11', '2023-11-11', 'шт', 'Anticoagulant blood thinner',            TRUE,  12.50, 19.00),
('Furosemide 40mg',     17, '2026-07-07', '2024-07-07', 'шт', 'Loop diuretic',                          TRUE,   7.00, 11.50),
('Prednisolone 5mg',    18, '2026-09-15', '2024-09-15', 'шт', 'Corticosteroid anti-inflammatory',       TRUE,   6.00, 10.50),
('Azithromycin 500mg',  19, '2026-01-20', '2024-01-20', 'шт', 'Macrolide antibiotic',                   TRUE,  15.00, 23.00),
('Diclofenac 50mg',     20, '2026-08-08', '2024-08-08', 'шт', 'NSAID anti-inflammatory',                FALSE,  5.50,  9.50);

INSERT INTO pharmacy (address, phone, working_hours) VALUES
('Chisinau, str. Stefan cel Mare 1',   '+37322001001', '08:00-20:00'),
('Chisinau, bd. Decebal 22',           '+37322001002', '08:00-21:00'),
('Chisinau, str. Puskin 14',           '+37322001003', '09:00-20:00'),
('Chisinau, bd. Moscovei 45',          '+37322001004', '08:00-22:00'),
('Chisinau, str. Ismail 77',           '+37322001005', '08:00-20:00'),
('Chisinau, bd. Riscani 3',            '+37322001006', '09:00-21:00'),
('Balti, str. Independentei 10',       '+37323101001', '08:00-20:00'),
('Balti, bd. Stefan cel Mare 55',      '+37323101002', '08:00-19:00'),
('Cahul, str. Republicii 8',           '+37329901001', '09:00-18:00'),
('Orhei, str. Vasile Mahu 12',         '+37323501001', '08:00-20:00'),
('Soroca, str. Stefan cel Mare 30',    '+37323001001', '08:00-19:00'),
('Ungheni, bd. National 4',            '+37323601001', '09:00-20:00'),
('Comrat, str. Lenin 17',              '+37329801001', '08:00-18:00'),
('Chisinau, str. Albisoara 88',        '+37322001007', '08:00-22:00'),
('Chisinau, str. Calea Iesilor 5',     '+37322001008', '09:00-21:00'),
('Chisinau, bd. Dacia 23',             '+37322001009', '08:00-20:00'),
('Chisinau, str. Florilor 11',         '+37322001010', '08:00-21:00'),
('Straseni, str. Mihai Eminescu 6',    '+37323701001', '09:00-19:00'),
('Ialoveni, str. Alexandru cel Bun 2', '+37326801001', '08:00-20:00'),
('Dubasari, str. Pacii 9',             '+37324801001', '09:00-18:00');

INSERT INTO employee (full_name, idnp, phone, address, salary, position) VALUES
('Andrei Popescu',     '2000101000001', '+37369000001', 'Chisinau, str. Trandafirilor 1',  8500.00, 'Pharmacist'),
('Maria Ionescu',      '2000101000002', '+37369000002', 'Chisinau, str. Rozelor 2',        7800.00, 'Pharmacist'),
('Ion Rusu',           '2000101000003', '+37369000003', 'Balti, str. Independentei 5',     7200.00, 'Cashier'),
('Elena Ciobanu',      '2000101000004', '+37369000004', 'Chisinau, bd. Decebal 10',        9000.00, 'Manager'),
('Vasile Moraru',      '2000101000005', '+37369000005', 'Chisinau, str. Stefan 3',         7500.00, 'Pharmacist'),
('Olga Lupascu',       '2000101000006', '+37369000006', 'Orhei, str. Mahu 7',              7000.00, 'Cashier'),
('Dumitru Grama',      '2000101000007', '+37369000007', 'Chisinau, str. Puskin 9',         8800.00, 'Pharmacist'),
('Natalia Botnari',    '2000101000008', '+37369000008', 'Soroca, str. Stefan 12',          7100.00, 'Cashier'),
('Sergiu Rata',        '2000101000009', '+37369000009', 'Cahul, str. Republicii 3',        8200.00, 'Pharmacist'),
('Cristina Popa',      '2000101000010', '+37369000010', 'Chisinau, bd. Dacia 5',           9500.00, 'Manager'),
('Alexandru Stoica',   '2000101000011', '+37369000011', 'Chisinau, str. Florilor 6',       7600.00, 'Pharmacist'),
('Tatiana Munteanu',   '2000101000012', '+37369000012', 'Ungheni, bd. National 1',         7300.00, 'Cashier'),
('Mihai Cojocaru',     '2000101000013', '+37369000013', 'Chisinau, str. Albisoara 4',      8400.00, 'Pharmacist'),
('Inna Vrabie',        '2000101000014', '+37369000014', 'Comrat, str. Lenin 5',            7000.00, 'Cashier'),
('Petru Negru',        '2000101000015', '+37369000015', 'Chisinau, str. Calea Iesilor 2',  8700.00, 'Pharmacist'),
('Alina Sirbu',        '2000101000016', '+37369000016', 'Straseni, str. Eminescu 3',       7400.00, 'Pharmacist'),
('Radu Filipescu',     '2000101000017', '+37369000017', 'Chisinau, str. Ismail 5',         9200.00, 'Manager'),
('Viorica Postolachi', '2000101000018', '+37369000018', 'Ialoveni, str. Alexandru 4',      7100.00, 'Cashier'),
('Gheorghe Damaschin', '2000101000019', '+37369000019', 'Chisinau, bd. Moscovei 2',        8300.00, 'Pharmacist'),
('Ludmila Taranu',     '2000101000020', '+37369000020', 'Dubasari, str. Pacii 4',          7000.00, 'Cashier');

INSERT INTO receipt (receipt_number, pharmacy_id, employee_id, total_amount, date, time) VALUES
(1001, 1,  1,  0, '2025-01-05', '09:15:00'),
(1002, 2,  2,  0, '2025-01-07', '10:30:00'),
(1003, 3,  3,  0, '2025-01-10', '11:00:00'),
(1004, 4,  4,  0, '2025-01-12', '12:45:00'),
(1005, 5,  5,  0, '2025-01-15', '14:00:00'),
(1006, 6,  6,  0, '2025-01-18', '15:20:00'),
(1007, 7,  7,  0, '2025-01-20', '09:00:00'),
(1008, 8,  8,  0, '2025-01-22', '10:10:00'),
(1009, 9,  9,  0, '2025-02-01', '11:30:00'),
(1010, 10, 10, 0, '2025-02-03', '13:00:00'),
(1011, 11, 11, 0, '2025-02-05', '14:15:00'),
(1012, 12, 12, 0, '2025-02-07', '09:45:00'),
(1013, 13, 13, 0, '2025-02-10', '10:00:00'),
(1014, 14, 14, 0, '2025-02-12', '11:20:00'),
(1015, 15, 15, 0, '2025-02-14', '12:00:00'),
(1016, 16, 16, 0, '2025-02-16', '13:30:00'),
(1017, 17, 17, 0, '2025-02-18', '14:50:00'),
(1018, 18, 18, 0, '2025-02-20', '09:30:00'),
(1019, 19, 19, 0, '2025-02-22', '10:45:00'),
(1020, 20, 20, 0, '2025-02-24', '11:15:00');

INSERT INTO order_item (receipt_id, product_id, quantity, discount) VALUES
(1,  1,  3, 0),
(2,  2,  1, 5),
(3,  3,  2, 0),
(4,  4,  4, 10),
(5,  5,  2, 0),
(6,  6,  1, 0),
(7,  7,  3, 5),
(8,  8,  5, 0),
(9,  9,  2, 10),
(10, 10, 4, 0),
(11, 11, 1, 0),
(12, 12, 2, 5),
(13, 13, 1, 0),
(14, 14, 3, 10),
(15, 15, 2, 0),
(16, 16, 1, 5),
(17, 17, 4, 0),
(18, 18, 2, 0),
(19, 19, 1, 10),
(20, 20, 3, 0);

UPDATE receipt r
SET total_amount = (
    SELECT COALESCE(SUM(total_price), 0)
    FROM order_item
    WHERE receipt_id = r.receipt_id
);

INSERT INTO stock_balance (pharmacy_id, product_id, remaining_qty) VALUES
(1,  1,  150),
(2,  2,   80),
(3,  3,  200),
(4,  4,   60),
(5,  5,  120),
(6,  6,   90),
(7,  7,  175),
(8,  8,  300),
(9,  9,   50),
(10, 10, 220),
(11, 11,  70),
(12, 12, 100),
(13, 13,  40),
(14, 14, 130),
(15, 15,  85),
(16, 16,  55),
(17, 17, 160),
(18, 18,  95),
(19, 19,  45),
(20, 20, 210);



CREATE OR REPLACE PROCEDURE sp_add_manufacturer(
    p_name    VARCHAR, p_country VARCHAR,
    p_address VARCHAR, p_phone   VARCHAR, p_email VARCHAR,
    OUT p_id INT
)
LANGUAGE plpgsql AS $$
BEGIN
    INSERT INTO manufacturer(name, country, address, phone, email)
    VALUES (p_name, p_country, p_address, p_phone, p_email)
    RETURNING manufacturer_id INTO p_id;
END;
$$;
 
CREATE OR REPLACE PROCEDURE sp_update_manufacturer(
    p_id INT, p_name VARCHAR, p_country VARCHAR,
    p_address VARCHAR, p_phone VARCHAR, p_email VARCHAR
)
LANGUAGE plpgsql AS $$
BEGIN
    UPDATE manufacturer
    SET name=p_name, country=p_country, address=p_address,
        phone=p_phone, email=p_email
    WHERE manufacturer_id = p_id;
    IF NOT FOUND THEN
        RAISE EXCEPTION 'Производитель с id=% не найден', p_id;
    END IF;
END;
$$;
 
-- Каскадное удаление: manufacturer → product → order_item, stock_balance
CREATE OR REPLACE PROCEDURE sp_delete_manufacturer(p_id INT)
LANGUAGE plpgsql AS $$
BEGIN
    DELETE FROM manufacturer
    WHERE manufacturer_id = p_id;

    IF NOT FOUND THEN
        RAISE EXCEPTION 'Производитель с id=% не найден', p_id;
    END IF;
END;
$$;
 
-- ──────────────────────────────────────────────────────────────────
-- PRODUCT
-- ──────────────────────────────────────────────────────────────────
 
CREATE OR REPLACE PROCEDURE sp_add_product(
    p_name VARCHAR, p_manufacturer_id INT,
    p_production_date DATE, p_expiration_date DATE,
    p_unit VARCHAR, p_description TEXT,
    p_prescription_required BOOLEAN,
    p_purchase_price NUMERIC, p_sale_price NUMERIC,
    OUT p_id INT
)
LANGUAGE plpgsql AS $$
BEGIN
    INSERT INTO product(name, manufacturer_id, production_date, expiration_date,
                        unit, description, prescription_required, purchase_price, sale_price)
    VALUES (p_name, p_manufacturer_id, p_production_date, p_expiration_date,
            p_unit, p_description, p_prescription_required, p_purchase_price, p_sale_price)
    RETURNING product_id INTO p_id;
END;
$$;
 
CREATE OR REPLACE PROCEDURE sp_update_product(
    p_id INT, p_name VARCHAR, p_manufacturer_id INT,
    p_production_date DATE, p_expiration_date DATE,
    p_unit VARCHAR, p_description TEXT,
    p_prescription_required BOOLEAN,
    p_purchase_price NUMERIC, p_sale_price NUMERIC
)
LANGUAGE plpgsql AS $$
BEGIN
    UPDATE product
    SET name=p_name, manufacturer_id=p_manufacturer_id,
        production_date=p_production_date, expiration_date=p_expiration_date,
        unit=p_unit, description=p_description,
        prescription_required=p_prescription_required,
        purchase_price=p_purchase_price, sale_price=p_sale_price
    WHERE product_id = p_id;
    IF NOT FOUND THEN
        RAISE EXCEPTION 'Товар с id=% не найден', p_id;
    END IF;
END;
$$;
 
-- Каскадное удаление: product → order_item, stock_balance
CREATE OR REPLACE PROCEDURE sp_delete_product(p_id INT)
LANGUAGE plpgsql AS $$
BEGIN
    DELETE FROM product       WHERE product_id = p_id;
    IF NOT FOUND THEN
        RAISE EXCEPTION 'Товар с id=% не найден', p_id;
    END IF;
END;
$$;
 
-- ──────────────────────────────────────────────────────────────────
-- PHARMACY
-- ──────────────────────────────────────────────────────────────────
 
CREATE OR REPLACE PROCEDURE sp_add_pharmacy(
    p_address VARCHAR, p_phone VARCHAR, p_working_hours VARCHAR,
    OUT p_id INT
)
LANGUAGE plpgsql AS $$
BEGIN
    INSERT INTO pharmacy(address, phone, working_hours)
    VALUES (p_address, p_phone, p_working_hours)
    RETURNING pharmacy_id INTO p_id;
END;
$$;
 
CREATE OR REPLACE PROCEDURE sp_update_pharmacy(
    p_id INT, p_address VARCHAR, p_phone VARCHAR, p_working_hours VARCHAR
)
LANGUAGE plpgsql AS $$
BEGIN
    UPDATE pharmacy
    SET address=p_address, phone=p_phone, working_hours=p_working_hours
    WHERE pharmacy_id = p_id;
    IF NOT FOUND THEN
        RAISE EXCEPTION 'Аптека с id=% не найдена', p_id;
    END IF;
END;
$$;
 
-- Каскадное удаление: pharmacy → receipt → order_item; pharmacy → stock_balance
CREATE OR REPLACE PROCEDURE sp_delete_pharmacy(p_id INT)
LANGUAGE plpgsql AS $$
BEGIN
    DELETE FROM pharmacy
    WHERE pharmacy_id = p_id;

    IF NOT FOUND THEN
        RAISE EXCEPTION 'Аптека с id=% не найдена', p_id;
    END IF;
END;
$$;
 
-- ──────────────────────────────────────────────────────────────────
-- EMPLOYEE
-- ──────────────────────────────────────────────────────────────────
 
CREATE OR REPLACE PROCEDURE sp_add_employee(
    p_full_name VARCHAR, p_idnp VARCHAR, p_phone VARCHAR,
    p_address VARCHAR, p_salary NUMERIC, p_position VARCHAR,
    OUT p_id INT
)
LANGUAGE plpgsql AS $$
BEGIN
    INSERT INTO employee(full_name, idnp, phone, address, salary, position)
    VALUES (p_full_name, p_idnp, p_phone, p_address, p_salary, p_position)
    RETURNING employee_id INTO p_id;
END;
$$;
 
CREATE OR REPLACE PROCEDURE sp_update_employee(
    p_id INT, p_full_name VARCHAR, p_idnp VARCHAR, p_phone VARCHAR,
    p_address VARCHAR, p_salary NUMERIC, p_position VARCHAR
)
LANGUAGE plpgsql AS $$
BEGIN
    UPDATE employee
    SET full_name=p_full_name, idnp=p_idnp, phone=p_phone,
        address=p_address, salary=p_salary, position=p_position
    WHERE employee_id = p_id;
    IF NOT FOUND THEN
        RAISE EXCEPTION 'Сотрудник с id=% не найден', p_id;
    END IF;
END;
$$;
 
-- Каскадное удаление: employee → receipt → order_item
CREATE OR REPLACE PROCEDURE sp_delete_employee(p_id INT)
LANGUAGE plpgsql AS $$
BEGIN
    DELETE FROM employee
    WHERE employee_id = p_id;

    IF NOT FOUND THEN
        RAISE EXCEPTION 'Сотрудник с id=% не найден', p_id;
    END IF;
END;
$$;
 
-- ──────────────────────────────────────────────────────────────────
-- RECEIPT
-- ──────────────────────────────────────────────────────────────────
 
CREATE OR REPLACE PROCEDURE sp_add_receipt(
    p_receipt_number INT, p_pharmacy_id INT, p_employee_id INT,
    p_date DATE, p_time TIME,
    OUT p_id INT
)
LANGUAGE plpgsql AS $$
BEGIN
    INSERT INTO receipt(receipt_number, pharmacy_id, employee_id, total_amount, date, time)
    VALUES (p_receipt_number, p_pharmacy_id, p_employee_id, 0, p_date, p_time)
    RETURNING receipt_id INTO p_id;
END;
$$;
 
CREATE OR REPLACE PROCEDURE sp_update_receipt(
    p_id INT, p_receipt_number INT, p_pharmacy_id INT,
    p_employee_id INT, p_date DATE, p_time TIME
)
LANGUAGE plpgsql AS $$
BEGIN
    UPDATE receipt
    SET receipt_number=p_receipt_number, pharmacy_id=p_pharmacy_id,
        employee_id=p_employee_id, date=p_date, time=p_time
    WHERE receipt_id = p_id;
    IF NOT FOUND THEN
        RAISE EXCEPTION 'Чек с id=% не найден', p_id;
    END IF;
END;
$$;
 
-- Каскадное удаление: receipt → order_item
CREATE OR REPLACE PROCEDURE sp_delete_receipt(p_id INT)
LANGUAGE plpgsql AS $$
BEGIN
    DELETE FROM receipt
    WHERE receipt_id = p_id;

    IF NOT FOUND THEN
        RAISE EXCEPTION 'Чек с id=% не найден', p_id;
    END IF;
END;
$$;
-- ──────────────────────────────────────────────────────────────────
-- ORDER ITEM
-- ──────────────────────────────────────────────────────────────────
 
CREATE OR REPLACE PROCEDURE sp_add_order_item(
    p_receipt_id INT, p_product_id INT,
    p_quantity INT, p_discount NUMERIC
)
LANGUAGE plpgsql AS $$
BEGIN
    -- unit_price заполнится триггером trg_set_unit_price
    INSERT INTO order_item(receipt_id, product_id, quantity, discount)
    VALUES (p_receipt_id, p_product_id, p_quantity, p_discount);
 
    -- Обновляем total_amount чека
    UPDATE receipt
    SET total_amount = (
        SELECT COALESCE(SUM(total_price), 0)
        FROM order_item WHERE receipt_id = p_receipt_id
    )
    WHERE receipt_id = p_receipt_id;
END;
$$;
 
CREATE OR REPLACE PROCEDURE sp_update_order_item(
    p_receipt_id INT, p_product_id INT,
    p_new_receipt_id INT, p_new_product_id INT,
    p_quantity INT, p_discount NUMERIC
)
LANGUAGE plpgsql AS $$
DECLARE
    v_price NUMERIC;
BEGIN
    -- Если меняется ключ — удаляем старую запись и вставляем новую
    IF p_receipt_id != p_new_receipt_id OR p_product_id != p_new_product_id THEN
        DELETE FROM order_item WHERE receipt_id = p_receipt_id AND product_id = p_product_id;
        SELECT sale_price INTO v_price FROM product WHERE product_id = p_new_product_id;
        INSERT INTO order_item(receipt_id, product_id, quantity, unit_price, discount)
        VALUES (p_new_receipt_id, p_new_product_id, p_quantity, v_price, p_discount);
    ELSE
        UPDATE order_item
        SET quantity=p_quantity, discount=p_discount
        WHERE receipt_id = p_receipt_id AND product_id = p_product_id;
    END IF;
 
    -- Пересчитываем total_amount для обоих чеков
    UPDATE receipt
    SET total_amount = (
        SELECT COALESCE(SUM(total_price), 0)
        FROM order_item WHERE receipt_id = receipt.receipt_id
    )
    WHERE receipt_id IN (p_receipt_id, p_new_receipt_id);
END;
$$;
 
CREATE OR REPLACE PROCEDURE sp_delete_order_item(
    p_receipt_id INT,
    p_product_id INT
)
LANGUAGE plpgsql AS $$
BEGIN
    DELETE FROM order_item
    WHERE receipt_id = p_receipt_id
      AND product_id = p_product_id;

    IF NOT FOUND THEN
        RAISE EXCEPTION 'Позиция не найдена (receipt_id=%, product_id=%)',
        p_receipt_id, p_product_id;
    END IF;

    -- обновляем сумму чека
    UPDATE receipt
    SET total_amount = (
        SELECT COALESCE(SUM(total_price), 0)
        FROM order_item
        WHERE receipt_id = p_receipt_id
    )
    WHERE receipt_id = p_receipt_id;
END;
$$;
 
-- ──────────────────────────────────────────────────────────────────
-- STOCK BALANCE
-- ──────────────────────────────────────────────────────────────────
 
CREATE OR REPLACE PROCEDURE sp_add_stock_balance(
    p_pharmacy_id INT, p_product_id INT, p_remaining_qty INT
)
LANGUAGE plpgsql AS $$
BEGIN
    INSERT INTO stock_balance(pharmacy_id, product_id, remaining_qty)
    VALUES (p_pharmacy_id, p_product_id, p_remaining_qty);
END;
$$;
 
CREATE OR REPLACE PROCEDURE sp_update_stock_balance(
    p_pharmacy_id INT, p_product_id INT,
    p_new_pharmacy_id INT, p_new_product_id INT,
    p_remaining_qty INT
)
LANGUAGE plpgsql AS $$
BEGIN
    IF p_pharmacy_id != p_new_pharmacy_id OR p_product_id != p_new_product_id THEN
        DELETE FROM stock_balance WHERE pharmacy_id = p_pharmacy_id AND product_id = p_product_id;
        INSERT INTO stock_balance(pharmacy_id, product_id, remaining_qty)
        VALUES (p_new_pharmacy_id, p_new_product_id, p_remaining_qty);
    ELSE
        UPDATE stock_balance
        SET remaining_qty = p_remaining_qty
        WHERE pharmacy_id = p_pharmacy_id AND product_id = p_product_id;
    END IF;
END;
$$;
 
CREATE OR REPLACE PROCEDURE sp_delete_stock_balance(
    p_pharmacy_id INT,
    p_product_id INT
)
LANGUAGE plpgsql AS $$
BEGIN
    DELETE FROM stock_balance
    WHERE pharmacy_id = p_pharmacy_id
      AND product_id = p_product_id;

    IF NOT FOUND THEN
        RAISE EXCEPTION 'Остаток не найден (pharmacy_id=%, product_id=%)',
        p_pharmacy_id, p_product_id;
    END IF;
END;
$$;