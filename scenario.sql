-- ══════════════════════════════════════════════════════════════════
-- HIPPOCRATES — Полный сценарий базы данных
-- ══════════════════════════════════════════════════════════════════
CREATE TABLE category (
    category_id   SERIAL PRIMARY KEY,
    name          VARCHAR(100) NOT NULL UNIQUE,
    description   TEXT
);

CREATE TABLE role (
    role_id       SERIAL PRIMARY KEY,
    name          VARCHAR(100) NOT NULL UNIQUE,
    fixed_salary  NUMERIC(10,2) NOT NULL CHECK (fixed_salary > 0),
    description   TEXT
);

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
    category_id           INT           NOT NULL REFERENCES category(category_id),
    manufacturer_id       INT           NOT NULL REFERENCES manufacturer(manufacturer_id) ON DELETE CASCADE,
    expiration_date       DATE          NOT NULL,
    production_date       DATE          NOT NULL,
    unit                  VARCHAR(50)   DEFAULT 'шт',
    description           TEXT,
    file_path             TEXT          DEFAULT NULL,
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
    role_id     INT           NOT NULL REFERENCES role(role_id)
);

CREATE TABLE system_users (
    user_id       SERIAL PRIMARY KEY,
    employee_id   INT          NOT NULL UNIQUE REFERENCES employee(employee_id) ON DELETE CASCADE,
    login         VARCHAR(100) NOT NULL UNIQUE,
    password_hash VARCHAR(255) NOT NULL,
    system_role   VARCHAR(10)  NOT NULL DEFAULT 'user'
                               CHECK (system_role IN ('admin', 'user')),
    is_active     BOOLEAN      NOT NULL DEFAULT TRUE
);

CREATE TABLE receipt (
    receipt_id     SERIAL PRIMARY KEY,
    receipt_number INT           NOT NULL,
    pharmacy_id    INT           NOT NULL REFERENCES pharmacy(pharmacy_id) ON DELETE CASCADE,
    employee_id    INT           NOT NULL REFERENCES employee(employee_id) ON DELETE CASCADE,
    total_amount   NUMERIC(10,2) NOT NULL DEFAULT 0,
    date           DATE          NOT NULL DEFAULT CURRENT_DATE,
    time           TIME          NOT NULL DEFAULT CURRENT_TIME,
    CONSTRAINT receipt_pharmacy_number_date_unique UNIQUE (pharmacy_id, receipt_number, date)
);

CREATE TABLE order_item (
    receipt_id  INT           NOT NULL REFERENCES receipt(receipt_id) ON DELETE CASCADE,
    product_id  INT           NOT NULL REFERENCES product(product_id) ON DELETE CASCADE,
    quantity    INT           NOT NULL CHECK (quantity > 0),
    unit_price  NUMERIC(10,2) NOT NULL,
	discount    NUMERIC(5,2)  NOT NULL DEFAULT 0 CHECK (discount >= 0 AND discount <= 100),
    total_price NUMERIC(10,2) GENERATED ALWAYS AS (quantity * unit_price * (1 - discount / 100.0)) STORED,
    PRIMARY KEY (receipt_id, product_id)
);

CREATE TABLE stock_balance (
    pharmacy_id   INT NOT NULL REFERENCES pharmacy(pharmacy_id) ON DELETE CASCADE,
    product_id    INT NOT NULL REFERENCES product(product_id) ON DELETE CASCADE,
    remaining_qty INT NOT NULL DEFAULT 0 CHECK (remaining_qty >= 0),
    PRIMARY KEY (pharmacy_id, product_id)
);

-- ══════════════════════════════════════════════════════════════════
-- ТРИГГЕРЫ
-- ══════════════════════════════════════════════════════════════════

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

CREATE OR REPLACE FUNCTION set_system_role()
RETURNS TRIGGER AS $$
DECLARE
    v_role_name VARCHAR(100);
BEGIN
    SELECT r.name INTO v_role_name
    FROM employee e
    JOIN role r ON r.role_id = e.role_id
    WHERE e.employee_id = NEW.employee_id;

    IF v_role_name = 'Accountant' THEN
        NEW.system_role := 'admin';
    ELSE
        NEW.system_role := 'user';
    END IF;

    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER trg_set_system_role
BEFORE INSERT ON system_users
FOR EACH ROW
EXECUTE FUNCTION set_system_role();

-- ══════════════════════════════════════════════════════════════════
-- ДАННЫЕ
-- ══════════════════════════════════════════════════════════════════

TRUNCATE system_users, stock_balance, order_item, receipt,
         employee, pharmacy, product, manufacturer, role, category
RESTART IDENTITY CASCADE;

INSERT INTO category (name, description) VALUES
('Антибиотики',          'Препараты для борьбы с бактериальными инфекциями'),
('Обезболивающие',       'Анальгетики и противовоспалительные средства'),
('Сердечно-сосудистые',  'Препараты для лечения заболеваний сердца и сосудов'),
('Антидиабетические',    'Препараты для лечения сахарного диабета'),
('Антигистаминные',      'Препараты против аллергии'),
('Гастроэнтерология',    'Препараты для лечения ЖКТ'),
('Гормональные',         'Кортикостероиды и гормональные препараты'),
('Психиатрические',      'Антидепрессанты, антипсихотики, анксиолитики'),
('Неврологические',      'Противосудорожные и препараты при болезни Паркинсона'),
('Противовирусные',      'Препараты против вирусных инфекций'),
('Противогрибковые',     'Антифунгальные препараты'),
('Опиоидные анальгетики','Сильные обезболивающие на основе опиоидов'),
('Витамины и минералы',  'Витаминные и минеральные добавки'),
('Бронхолегочные',       'Препараты для лечения дыхательных путей'),
('Диуретики',            'Мочегонные препараты'),
('Антикоагулянты',       'Препараты для разжижения крови'),
('Противоподагрические', 'Препараты для лечения подагры'),
('НПВС',                 'Нестероидные противовоспалительные средства');

-- role_id: 1=Pharmacist, 2=Manager, 3=Cashier, 4=Accountant
INSERT INTO role (name, fixed_salary, description) VALUES
('Pharmacist',  9000.00, 'Фармацевт — отпуск лекарственных средств'),
('Manager',    12000.00, 'Управляющий аптекой'),
('Cashier',     7500.00, 'Кассир — приём оплаты'),
('Accountant', 13000.00, 'Бухгалтер — финансовый учёт, доступ admin');

INSERT INTO manufacturer (name, country, address, phone, email) VALUES
('Bayer AG','Germany','Leverkusen, Kaiser-Wilhelm-Allee 1','+4921430000','contact@bayer.com'),
('Pfizer Inc.','USA','New York, 235 E 42nd St','+12125732323','info@pfizer.com'),
('Novartis AG','Switzerland','Basel, Lichtstrasse 35','+41613241111','info@novartis.com'),
('Roche Holding AG','Switzerland','Basel, Grenzacherstrasse 124','+41616881111','contact@roche.com'),
('Sanofi S.A.','France','Paris, 54 Rue La Boetie','+33153773000','info@sanofi.com'),
('GlaxoSmithKline','UK','London, 980 Great West Rd','+442089908000','info@gsk.com'),
('AstraZeneca','UK','Cambridge, 1 Francis Crick Ave','+441223837000','contact@astrazeneca.com'),
('Johnson & Johnson','USA','New Jersey, One J&J Plaza','+17325242455','info@jnj.com'),
('Merck & Co.','USA','New Jersey, 2000 Galloping Hill Rd','+19087404000','info@merck.com'),
('Abbott Laboratories','USA','Illinois, 100 Abbott Park Rd','+18474377000','info@abbott.com'),
('Teva Pharmaceuticals','Israel','Tel Aviv, 5 Basel St','+97236267000','info@teva.com'),
('Actavis Group','Iceland','Reykjavik, Reykjavikurvegi 76','+3545503000','info@actavis.com'),
('KRKA d.d.','Slovenia','Novo Mesto, Smarjeska cesta 6','+38673311111','info@krka.si'),
('Gedeon Richter','Hungary','Budapest, Gyomroi ut 19-21','+3614314000','info@richter.hu'),
('Stada Arzneimittel','Germany','Bad Vilbel, Stadastrasse 2-18','+4961019500','info@stada.de'),
('Sandoz AG','Switzerland','Basel, Lichtstrasse 35','+41613246111','info@sandoz.com'),
('Recordati S.p.A.','Italy','Milan, Via Civitali 1','+390248787111','info@recordati.it'),
('Servier','France','Suresnes, 50 Rue Carnot','+33155726000','info@servier.com'),
('Boehringer Ingelheim','Germany','Ingelheim, Binger Strasse 173','+496132770','info@boehringer.com'),
('Zentiva Group','Czech Rep.','Prague, U kabelovny 130','+420261091111','info@zentiva.com'),
('Cipla Ltd.','India','Mumbai, Peninsula Business Park','+912224826000','info@cipla.com'),
('Sun Pharma','India','Mumbai, Acme Plaza Andheri','+912266455645','info@sunpharma.com'),
('Dr. Reddys Labs','India','Hyderabad, 8-2-337 Road No.3','+914023431422','info@drreddys.com'),
('Hikma Pharmaceuticals','Jordan','Amman, 226 Airport Rd','+96265819999','info@hikma.com'),
('Almirall S.A.','Spain','Barcelona, Ronda General Mitre 151','+34934916000','info@almirall.com'),
('Menarini Group','Italy','Florence, Via Sette Santi 3','+390555680','info@menarini.com'),
('Pierre Fabre','France','Castres, 45 Place Abel Gance','+33563716000','info@pierre-fabre.com'),
('Fresenius Kabi','Germany','Bad Homburg, Else-Kroener-Str 1','+496172860','info@fresenius-kabi.com'),
('Biocon Ltd.','India','Bangalore, 20th KM Hosur Rd','+918028082808','info@biocon.com'),
('Egis Pharmaceuticals','Hungary','Budapest, Kereszturi ut 30-38','+3614650900','info@egis.hu');

-- category_id: 1=Антибиотики 2=Обезболивающие 3=Сердечно-сосудистые
-- 4=Антидиабетические 5=Антигистаминные 6=Гастроэнтерология
-- 7=Гормональные 8=Психиатрические 9=Неврологические 10=Противовирусные
-- 11=Противогрибковые 12=Опиоидные 13=Витамины 14=Бронхолегочные
-- 15=Диуретики 16=Антикоагулянты 17=Противоподагрические 18=НПВС
INSERT INTO product (name, category_id, manufacturer_id, expiration_date, production_date, unit, description, prescription_required, purchase_price, sale_price) VALUES
('Aspirin 500mg',           2, 1,'2025-04-25','2024-10-16','шт','Pain reliever and fever reducer',FALSE,5.0,8.5),
('Amoxicillin 500mg',       1, 2,'2025-10-09','2023-01-26','шт','Antibiotic for bacterial infections',TRUE,12.0,18.0),
('Ibuprofen 400mg',        18, 3,'2025-08-17','2023-09-08','шт','Anti-inflammatory painkiller',FALSE,6.5,10.0),
('Metformin 850mg',         4, 4,'2025-04-15','2023-05-23','шт','Diabetes type 2 treatment',TRUE,8.0,13.5),
('Lisinopril 10mg',         3, 5,'2026-07-13','2024-11-23','шт','ACE inhibitor for blood pressure',TRUE,9.0,15.0),
('Atorvastatin 20mg',       3, 6,'2026-08-28','2023-03-31','шт','Cholesterol-lowering statin',TRUE,14.0,22.0),
('Omeprazole 20mg',         6, 7,'2025-02-02','2024-03-08','шт','Proton pump inhibitor for acid reflux',FALSE,7.5,12.0),
('Paracetamol 500mg',       2, 8,'2025-04-06','2023-01-31','шт','Analgesic and antipyretic',FALSE,3.5,6.0),
('Losartan 50mg',           3, 9,'2025-08-27','2023-08-12','шт','Angiotensin receptor blocker',TRUE,10.5,17.0),
('Cetirizine 10mg',         5,10,'2026-09-09','2024-06-01','шт','Antihistamine for allergies',FALSE,5.5,9.0),
('Doxycycline 100mg',       1,11,'2026-07-29','2023-01-28','шт','Broad-spectrum antibiotic',TRUE,11.0,16.5),
('Simvastatin 40mg',        3,12,'2026-10-28','2023-07-23','шт','Statin for cholesterol reduction',TRUE,13.0,20.0),
('Clopidogrel 75mg',       16,13,'2026-07-13','2024-12-19','шт','Antiplatelet agent',TRUE,16.0,25.0),
('Pantoprazole 40mg',       6,14,'2025-08-14','2024-03-05','шт','Proton pump inhibitor',FALSE,8.5,14.0),
('Amlodipine 5mg',          3,15,'2026-08-27','2024-04-04','шт','Calcium channel blocker',TRUE,9.5,15.5),
('Warfarin 5mg',           16,16,'2025-01-07','2023-10-12','шт','Anticoagulant blood thinner',TRUE,12.5,19.0),
('Furosemide 40mg',        15,17,'2026-12-16','2023-06-13','шт','Loop diuretic',TRUE,7.0,11.5),
('Prednisolone 5mg',        7,18,'2025-12-15','2024-03-08','шт','Corticosteroid anti-inflammatory',TRUE,6.0,10.5),
('Azithromycin 500mg',      1,19,'2025-06-09','2023-10-12','шт','Macrolide antibiotic',TRUE,15.0,23.0),
('Diclofenac 50mg',        18,20,'2025-12-11','2023-08-09','шт','NSAID anti-inflammatory',FALSE,5.5,9.5),
('Ciprofloxacin 500mg',     1,21,'2025-04-05','2023-04-15','шт','Fluoroquinolone antibiotic',TRUE,13.0,20.0),
('Metronidazole 400mg',     1,22,'2025-04-10','2024-01-25','шт','Antiprotozoal antibiotic',TRUE,6.0,9.5),
('Enalapril 10mg',          3,23,'2025-12-19','2024-01-03','шт','ACE inhibitor',TRUE,8.5,14.0),
('Ramipril 5mg',            3,24,'2025-09-28','2024-09-10','шт','ACE inhibitor',TRUE,9.0,15.0),
('Bisoprolol 5mg',          3,25,'2026-04-16','2023-02-14','шт','Beta blocker',TRUE,10.0,16.0),
('Carvedilol 25mg',         3,26,'2025-05-08','2024-07-03','шт','Beta blocker',TRUE,11.5,18.0),
('Valsartan 80mg',          3,27,'2025-03-22','2024-01-23','шт','ARB antihypertensive',TRUE,12.0,19.0),
('Hydrochlorothiazide 25mg',15,28,'2025-10-28','2024-07-19','шт','Thiazide diuretic',TRUE,5.0,8.0),
('Spironolactone 50mg',    15,29,'2026-09-26','2024-10-05','шт','Potassium-sparing diuretic',TRUE,8.0,13.0),
('Digoxin 0.25mg',          3,30,'2026-08-15','2024-01-06','шт','Cardiac glycoside',TRUE,7.5,12.5),
('Allopurinol 300mg',      17, 1,'2026-12-23','2023-07-16','шт','Gout treatment xanthine oxidase inhibitor',FALSE,6.0,10.0),
('Colchicine 0.5mg',       17, 2,'2025-02-16','2023-03-13','шт','Anti-gout agent',TRUE,14.0,22.0),
('Levothyroxine 100mcg',    7, 3,'2025-08-22','2024-11-08','шт','Thyroid hormone replacement',TRUE,7.0,11.5),
('Insulin Glargine 300IU',  4, 4,'2025-03-23','2023-10-24','шт','Long-acting insulin',TRUE,45.0,70.0),
('Metoprolol 50mg',         3, 5,'2025-04-14','2023-08-27','шт','Beta-1 selective blocker',TRUE,9.0,14.5),
('Propranolol 40mg',        3, 6,'2025-10-12','2024-01-25','шт','Beta blocker',TRUE,7.5,12.0),
('Nifedipine 20mg',         3, 7,'2026-10-13','2024-04-09','шт','Calcium channel blocker',TRUE,8.0,13.0),
('Diltiazem 60mg',          3, 8,'2025-06-16','2024-01-09','шт','Calcium channel blocker',TRUE,10.0,16.5),
('Verapamil 80mg',          3, 9,'2025-12-30','2024-01-15','шт','Calcium channel blocker',TRUE,11.0,17.0),
('Nitroglycerin 0.5mg',     3,10,'2026-11-18','2023-08-03','шт','Antianginal nitrate',TRUE,16.0,25.0),
('Isosorbide 20mg',         3,11,'2026-12-20','2023-10-01','шт','Organic nitrate',TRUE,12.0,19.0),
('Cephalexin 500mg',        1,12,'2026-10-26','2024-11-30','шт','First-gen cephalosporin',TRUE,13.0,20.0),
('Clindamycin 300mg',       1,13,'2026-09-16','2023-03-15','шт','Lincosamide antibiotic',TRUE,14.0,22.0),
('Erythromycin 500mg',      1,14,'2025-06-25','2024-10-12','шт','Macrolide antibiotic',TRUE,12.0,19.0),
('Clarithromycin 500mg',    1,15,'2025-09-08','2024-06-30','шт','Macrolide antibiotic',TRUE,16.0,25.0),
('Tetracycline 500mg',      1,16,'2026-04-19','2023-06-17','шт','Broad-spectrum antibiotic',TRUE,8.0,13.0),
('Trimethoprim 200mg',      1,17,'2025-10-04','2024-01-24','шт','Antibiotic',TRUE,7.0,11.0),
('Nitrofurantoin 100mg',    1,18,'2026-12-06','2024-10-17','шт','Urinary antibiotic',TRUE,9.0,14.5),
('Fluconazole 150mg',      11,19,'2025-08-13','2024-07-24','шт','Antifungal',FALSE,10.0,16.0),
('Ketoconazole 200mg',     11,20,'2025-11-29','2024-12-02','шт','Antifungal',TRUE,9.0,14.5),
('Acyclovir 400mg',        10,21,'2025-08-23','2023-02-27','шт','Antiviral',FALSE,11.0,17.5),
('Oseltamivir 75mg',       10,22,'2025-11-20','2023-02-02','шт','Influenza antiviral',TRUE,25.0,38.0),
('Hydroxychloroquine 200mg',10,23,'2025-10-02','2024-02-15','шт','Antimalarial DMARD',TRUE,18.0,28.0),
('Sulfasalazine 500mg',    18,24,'2025-08-05','2023-03-09','шт','Anti-inflammatory DMARD',TRUE,14.0,22.0),
('Methotrexate 10mg',       7,25,'2025-11-19','2024-08-03','шт','DMARD chemotherapy',TRUE,22.0,34.0),
('Prednisone 5mg',          7,26,'2026-11-03','2023-08-06','шт','Corticosteroid',TRUE,6.5,10.5),
('Dexamethasone 4mg',       7,27,'2026-02-10','2024-05-26','шт','Corticosteroid',TRUE,7.0,11.5),
('Budesonide 200mcg',      14,28,'2026-04-15','2024-10-20','шт','Inhaled corticosteroid',TRUE,20.0,31.0),
('Salbutamol 100mcg',      14,29,'2025-09-29','2023-05-27','шт','Beta-2 agonist bronchodilator',FALSE,11.0,17.0),
('Salmeterol 50mcg',       14,30,'2025-09-10','2023-05-23','шт','Long-acting beta-2 agonist',TRUE,22.0,34.0),
('Montelukast 10mg',       14, 1,'2026-07-06','2024-07-28','шт','Leukotriene receptor antagonist',FALSE,15.0,23.0),
('Ipratropium 20mcg',      14, 2,'2026-08-22','2023-09-27','шт','Anticholinergic bronchodilator',TRUE,18.0,28.0),
('Esomeprazole 40mg',       6, 3,'2026-08-21','2024-03-14','шт','Proton pump inhibitor',FALSE,9.0,14.5),
('Lansoprazole 30mg',       6, 4,'2026-01-06','2024-02-13','шт','Proton pump inhibitor',FALSE,8.0,13.0),
('Ranitidine 150mg',        6, 5,'2025-05-22','2023-08-13','шт','H2 receptor antagonist',FALSE,6.0,9.5),
('Famotidine 20mg',         6, 6,'2026-05-21','2024-06-05','шт','H2 receptor antagonist',FALSE,7.0,11.0),
('Domperidone 10mg',        6, 7,'2025-02-18','2023-04-04','шт','Antiemetic prokinetic',FALSE,8.0,13.0),
('Metoclopramide 10mg',     6, 8,'2025-06-06','2023-04-23','шт','Antiemetic prokinetic',FALSE,5.5,9.0),
('Ondansetron 8mg',         6, 9,'2025-06-13','2024-10-04','шт','5-HT3 antiemetic',TRUE,12.0,19.0),
('Loperamide 2mg',          6,10,'2026-03-09','2024-11-27','шт','Antidiarrheal',FALSE,5.0,8.0),
('Bisacodyl 5mg',           6,11,'2025-03-07','2024-09-02','шт','Stimulant laxative',FALSE,4.0,6.5),
('Lactulose 10g',           6,12,'2026-01-26','2024-01-30','шт','Osmotic laxative',FALSE,6.0,9.5),
('Sertraline 50mg',         8,13,'2026-04-25','2024-09-02','шт','SSRI antidepressant',TRUE,16.0,25.0),
('Fluoxetine 20mg',         8,14,'2025-09-15','2024-06-25','шт','SSRI antidepressant',TRUE,14.0,22.0),
('Escitalopram 10mg',       8,15,'2025-01-12','2024-07-20','шт','SSRI antidepressant',TRUE,18.0,28.0),
('Amitriptyline 25mg',      8,16,'2025-04-28','2024-11-27','шт','Tricyclic antidepressant',TRUE,12.0,19.0),
('Diazepam 5mg',            8,17,'2026-07-04','2024-11-29','шт','Benzodiazepine anxiolytic',TRUE,10.0,16.0),
('Alprazolam 0.5mg',        8,18,'2026-10-19','2023-10-01','шт','Benzodiazepine',TRUE,13.0,20.0),
('Zolpidem 10mg',           8,19,'2025-04-25','2023-12-15','шт','Non-benzo hypnotic',TRUE,15.0,23.0),
('Quetiapine 100mg',        8,20,'2026-03-22','2023-10-28','шт','Atypical antipsychotic',TRUE,22.0,34.0),
('Risperidone 2mg',         8,21,'2026-04-10','2023-06-11','шт','Atypical antipsychotic',TRUE,20.0,31.0),
('Haloperidol 5mg',         8,22,'2025-09-27','2023-01-04','шт','Typical antipsychotic',TRUE,11.0,17.0),
('Levodopa 250mg',          9,23,'2025-07-02','2024-05-27','шт','Antiparkinsonian agent',TRUE,18.0,28.0),
('Gabapentin 300mg',        9,24,'2025-04-19','2024-06-03','шт','Anticonvulsant neuropathic pain',TRUE,14.0,22.0),
('Pregabalin 75mg',         9,25,'2025-11-02','2024-10-02','шт','Anticonvulsant neuropathic pain',TRUE,20.0,31.0),
('Carbamazepine 200mg',     9,26,'2026-06-04','2024-10-16','шт','Anticonvulsant mood stabilizer',TRUE,12.0,19.0),
('Valproate 500mg',         9,27,'2025-07-23','2024-09-15','шт','Anticonvulsant',TRUE,13.0,20.0),
('Phenobarbital 100mg',     9,28,'2026-01-18','2023-06-06','шт','Barbiturate anticonvulsant',TRUE,8.0,13.0),
('Tramadol 50mg',          12,29,'2026-07-07','2023-06-15','шт','Opioid analgesic',TRUE,14.0,22.0),
('Codeine 30mg',           12,30,'2025-01-01','2024-06-27','шт','Opioid analgesic cough suppressant',TRUE,12.0,19.0),
('Morphine 10mg',          12, 1,'2025-11-28','2024-09-05','шт','Opioid analgesic',TRUE,20.0,31.0),
('Fentanyl patch 25mcg',   12, 2,'2025-01-20','2024-05-15','шт','Transdermal opioid',TRUE,38.0,58.0),
('Naproxen 500mg',         18, 3,'2026-01-07','2023-04-25','шт','NSAID analgesic',FALSE,7.0,11.0),
('Meloxicam 15mg',         18, 4,'2025-09-03','2023-11-11','шт','Selective COX-2 NSAID',TRUE,10.0,16.0),
('Etoricoxib 90mg',        18, 5,'2025-09-04','2023-03-01','шт','COX-2 inhibitor',TRUE,16.0,25.0),
('Calcium carbonate 500mg',13, 6,'2025-03-22','2024-08-03','шт','Antacid calcium supplement',FALSE,4.0,6.5),
('Vitamin D3 1000IU',      13, 7,'2026-05-13','2023-03-29','шт','Vitamin supplement',FALSE,5.0,8.0),
('Folic acid 5mg',         13, 8,'2026-06-30','2023-03-12','шт','B-vitamin supplement',FALSE,3.5,5.5),
('Zinc sulfate 220mg',     13, 9,'2025-05-12','2023-05-09','шт','Mineral supplement',FALSE,4.5,7.0),
('Magnesium oxide 400mg',  13,10,'2026-05-02','2024-11-06','шт','Mineral supplement',FALSE,5.5,8.5);

INSERT INTO pharmacy (address, phone, working_hours) VALUES
('Chisinau, str. Stefan cel Mare 71','+37320002705','08:00-20:00'),
('Chisinau, bd. Decebal 34','+37320408645','08:00-21:00'),
('Chisinau, str. Puskin 112','+37320809938','09:00-20:00'),
('Chisinau, bd. Moscovei 55','+37321203470','08:00-22:00'),
('Chisinau, str. Ismail 119','+37321608835','09:00-21:00'),
('Chisinau, bd. Riscani 97','+37322003295','08:00-19:00'),
('Chisinau, str. Albisoara 92','+37322405107','09:00-18:00'),
('Chisinau, str. Calea Iesilor 52','+37322806118','08:00-20:00'),
('Chisinau, bd. Dacia 57','+37323208479','08:00-21:00'),
('Chisinau, str. Florilor 58','+37323601982','09:00-20:00'),
('Chisinau, str. Trandafirilor 32','+37324003681','08:00-22:00'),
('Chisinau, str. Rozelor 9','+37324405539','09:00-21:00'),
('Chisinau, bd. Cantemir 3','+37324809638','08:00-19:00'),
('Chisinau, str. Columna 71','+37325203770','09:00-18:00'),
('Chisinau, str. Armeneasca 76','+37325603608','08:00-20:00'),
('Chisinau, str. Mihai Eminescu 1','+37326001163','08:00-21:00'),
('Chisinau, bd. Stefan cel Mare 91','+37326400964','09:00-20:00'),
('Chisinau, str. Bucuresti 30','+37326801104','08:00-22:00'),
('Chisinau, str. Independentei 116','+37327200514','09:00-21:00'),
('Chisinau, str. Vasile Alecsandri 111','+37327605413','08:00-19:00'),
('Chisinau, str. Alexandru cel Bun 10','+37328008423','09:00-18:00'),
('Chisinau, bd. Mircea cel Batran 31','+37328404562','08:00-20:00'),
('Chisinau, str. Petricani 86','+37328807953','08:00-21:00'),
('Chisinau, str. Calea Orheiului 28','+37329208834','09:00-20:00'),
('Chisinau, str. Uzinelor 17','+37329609355','08:00-22:00'),
('Balti, str. Stefan cel Mare 74','+37330007744','09:00-21:00'),
('Balti, bd. Decebal 32','+37330407749','08:00-19:00'),
('Balti, str. Puskin 104','+37330806669','09:00-18:00'),
('Balti, bd. Moscovei 25','+37331201545','08:00-20:00'),
('Balti, str. Ismail 13','+37331607062','08:00-21:00'),
('Cahul, bd. Riscani 46','+37332006939','09:00-20:00'),
('Cahul, str. Albisoara 53','+37332407651','08:00-22:00'),
('Cahul, str. Calea Iesilor 111','+37332800887','09:00-21:00'),
('Orhei, bd. Dacia 87','+37333201612','08:00-19:00'),
('Orhei, str. Florilor 8','+37333606596','09:00-18:00'),
('Orhei, str. Trandafirilor 94','+37334005559','08:00-20:00'),
('Soroca, str. Rozelor 103','+37334401790','08:00-21:00'),
('Soroca, bd. Cantemir 32','+37334803139','09:00-20:00'),
('Ungheni, str. Columna 25','+37335208786','08:00-22:00'),
('Ungheni, str. Armeneasca 58','+37335602296','09:00-21:00'),
('Ungheni, str. Mihai Eminescu 55','+37336003006','08:00-19:00'),
('Comrat, bd. Stefan cel Mare 36','+37336407579','09:00-18:00'),
('Comrat, str. Bucuresti 32','+37336801235','08:00-20:00'),
('Comrat, str. Independentei 57','+37337209016','08:00-21:00'),
('Straseni, str. Vasile Alecsandri 13','+37337600828','09:00-20:00'),
('Straseni, str. Alexandru cel Bun 84','+37338008856','08:00-22:00'),
('Ialoveni, bd. Mircea cel Batran 108','+37338400241','09:00-21:00'),
('Ialoveni, str. Petricani 12','+37338803872','08:00-19:00'),
('Dubasari, str. Calea Orheiului 22','+37339206658','09:00-18:00'),
('Dubasari, str. Uzinelor 63','+37339607886','08:00-20:00');

-- role_id: 1=Pharmacist 2=Manager 3=Cashier 4=Accountant
INSERT INTO employee (full_name, idnp, phone, address, role_id) VALUES
('Andrei Popescu',   '2950105810049','+37360006570','Chisinau, str. Lupascu 4',   1),
('Maria Ionescu',    '2289439657508','+37360207454','Chisinau, str. Grama 19',    1),
('Ion Rusu',         '2807270532291','+37360409105','Chisinau, str. Botnari 43',  1),
('Elena Ciobanu',    '2206823277527','+37360604861','Chisinau, str. Rata 14',     3),
('Vasile Moraru',    '2809941412032','+37360808883','Chisinau, str. Popa 4',     2),
('Olga Lupascu',     '2051785130539','+37361009571','Chisinau, str. Stoica 31',   1),
('Dumitru Grama',    '2583482989002','+37361202579','Chisinau, str. Munteanu 4',  1),
('Natalia Botnari',  '2932351979347','+37361403044','Chisinau, str. Cojocaru 5',  1),
('Sergiu Rata',      '2947792820940','+37361603853','Chisinau, str. Vrabie 26',   3),
('Cristina Popa',    '2626594012825','+37361804033','Chisinau, str. Negru 38',   2),
('Alexandru Stoica', '2088559569253','+37362006868','Chisinau, str. Sirbu 43',    1),
('Tatiana Munteanu', '2345842718357','+37362204272','Chisinau, str. Filipescu 14',1),
('Mihai Cojocaru',   '2263342414490','+37362404351','Chisinau, str. Postolachi 26',1),
('Inna Vrabie',      '2329189919272','+37362607491','Chisinau, str. Damaschin 21',3),
('Petru Negru',      '2081328359515','+37362800152','Chisinau, str. Taranu 30',  2),
('Alina Sirbu',      '2077738827541','+37363008808','Chisinau, str. Lungu 14',    1),
('Radu Filipescu',   '2967866623129','+37363201127','Chisinau, str. Cebotari 16', 1),
('Viorica Postolachi','2481713854648','+37363408900','Chisinau, str. Ursu 46',    1),
('Gheorghe Damaschin','2720726006289','+37363608666','Chisinau, str. Danu 1',     3),
('Ludmila Taranu',   '2328799513174','+37363801697','Chisinau, str. Grosu 9',   2),
('Victor Lungu',     '2119785403991','+37364009064','Chisinau, str. Mija 10',     1),
('Diana Cebotari',   '2230230991019','+37364205617','Chisinau, str. Melniciuc 14',1),
('Constantin Ursu',  '2291426041482','+37364408280','Chisinau, str. Scutaru 32',  1),
('Irina Danu',       '2931613449336','+37364600832','Chisinau, str. Balan 6',     3),
('Pavel Grosu',      '2304209733660','+37364800722','Chisinau, str. Calin 1',    2),
('Svetlana Mija',    '2700641535392','+37365004291','Chisinau, str. Buza 11',     1),
('Grigore Melniciuc','2775463562649','+37365207007','Chisinau, str. Istrati 36',  1),
('Valentina Scutaru','2759707157168','+37365402442','Chisinau, str. Gherghel 35', 1),
('Marian Balan',     '2641535897688','+37365609052','Chisinau, str. Zadic 10',    3),
('Lidia Calin',      '2335187102686','+37365805974','Chisinau, str. Anghel 3',   2),
('Eugen Buza',       '2748226581356','+37366004088','Chisinau, str. Olaru 43',    1),
('Larisa Istrati',   '2617530897084','+37366206658','Chisinau, str. Rotaru 40',   1),
('Bogdan Gherghel',  '2950204551500','+37366402662','Chisinau, str. Chiper 12',   1),
('Galina Zadic',     '2193379984954','+37366605442','Chisinau, str. Vascan 27',   3),
('Adrian Anghel',    '2811164978375','+37366804065','Chisinau, str. Amariei 18', 2),
('Natalya Olaru',    '2118976083876','+37367006267','Chisinau, str. Popescu 3',   1),
('Liviu Rotaru',     '2219998677633','+37367207541','Chisinau, str. Ionescu 23',  1),
('Doina Chiper',     '2961193689894','+37367403728','Chisinau, str. Rusu 15',     1),
('Florin Vascan',    '2438916150327','+37367605378','Chisinau, str. Ciobanu 18',  3),
('Angela Amariei',   '2850261314384','+37367804573','Chisinau, str. Moraru 23',  2),
('Ciprian Popescu',  '2744745947806','+37368008785','Chisinau, str. Lupascu 22',  1),
('Mirela Ionescu',   '2962568063334','+37368204279','Chisinau, str. Grama 12',    1),
('Tudor Rusu',       '2291987126394','+37368400626','Chisinau, str. Botnari 7',   1),
('Corina Ciobanu',   '2800348631876','+37368605139','Chisinau, str. Rata 28',     3),
('Vlad Moraru',      '2126750596909','+37368806311','Chisinau, str. Popa 37',    2),
('Rodica Lupascu',   '2777579719274','+37369007144','Chisinau, str. Stoica 1',    1),
('Stefan Grama',     '2591874457344','+37369203228','Chisinau, str. Munteanu 24', 1),
('Nicoleta Botnari', '2734219825197','+37369405409','Chisinau, str. Cojocaru 40', 1),
('Oleg Rata',        '2136786184083','+37369604920','Chisinau, str. Vrabie 33',   3),
('Ala Popa',         '2358236319765','+37369806592','Chisinau, str. Negru 45',   4);

-- Пользователи системы
-- system_role проставится триггером автоматически:
-- employee_id=50 (Ala Popa, Accountant) -> admin, остальные -> user
INSERT INTO system_users (employee_id, login, password_hash) VALUES
(1,  'a.popescu',  md5('user123')),
(2,  'm.ionescu',  md5('user123')),
(3,  'i.rusu',     md5('user123')),
(4,  'e.ciobanu',  md5('user123')),
(5,  'v.moraru',   md5('user123')),
(6,  'o.lupascu',  md5('user123')),
(7,  'd.grama',    md5('user123')),
(8,  'n.botnari',  md5('user123')),
(9,  's.rata',     md5('user123')),
(10, 'c.popa',     md5('user123')),
(50, 'a.popa',     md5('admin123'));

INSERT INTO receipt (receipt_number, pharmacy_id, employee_id, total_amount, date, time) VALUES
(1, 1, 1,0,'2025-06-24','08:54:00'),
(1, 2, 2,0,'2025-08-04','15:06:00'),
(1, 3, 3,0,'2025-08-10','13:40:00'),
(1, 4, 4,0,'2025-08-24','10:27:00'),
(1, 5, 5,0,'2025-04-01','16:41:00'),
(1, 6, 6,0,'2025-05-19','17:51:00'),
(1, 7, 7,0,'2025-10-03','15:29:00'),
(1, 8, 8,0,'2025-08-12','17:17:00'),
(1, 9, 9,0,'2025-06-15','11:53:00'),
(1,10,10,0,'2025-02-14','12:56:00'),
(1,11,11,0,'2025-08-19','11:48:00'),
(1,12,12,0,'2025-08-26','17:39:00'),
(1,13,13,0,'2025-12-09','14:21:00'),
(1,14,14,0,'2025-01-15','15:54:00'),
(1,15,15,0,'2025-06-16','10:31:00'),
(1,16,16,0,'2025-04-19','13:51:00'),
(1,17,17,0,'2025-05-13','13:17:00'),
(1,18,18,0,'2025-11-02','12:35:00'),
(1,19,19,0,'2025-01-06','16:12:00'),
(1,20,20,0,'2025-02-13','11:46:00'),
(1,21,21,0,'2025-07-28','15:35:00'),
(1,22,22,0,'2025-05-04','15:41:00'),
(1,23,23,0,'2025-12-31','15:28:00'),
(1,24,24,0,'2025-01-09','09:18:00'),
(1,25,25,0,'2025-04-24','14:44:00'),
(1,26,26,0,'2025-05-05','12:42:00'),
(1,27,27,0,'2025-10-25','13:30:00'),
(1,28,28,0,'2025-10-11','16:22:00'),
(1,29,29,0,'2025-08-06','16:21:00'),
(1,30,30,0,'2025-06-30','15:17:00'),
(1,31,31,0,'2025-06-06','12:14:00'),
(1,32,32,0,'2025-03-03','11:20:00'),
(1,33,33,0,'2025-03-03','16:48:00'),
(1,34,34,0,'2025-12-20','10:12:00'),
(1,35,35,0,'2025-04-21','15:17:00'),
(1,36,36,0,'2025-10-29','16:38:00'),
(1,37,37,0,'2025-05-25','09:53:00'),
(1,38,38,0,'2025-04-10','12:14:00'),
(1,39,39,0,'2025-07-04','10:19:00'),
(1,40,40,0,'2025-01-08','16:08:00'),
(1,41,41,0,'2025-05-21','08:03:00'),
(1,42,42,0,'2025-10-11','12:44:00'),
(1,43,43,0,'2025-03-06','18:55:00'),
(1,44,44,0,'2025-09-09','09:55:00'),
(1,45,45,0,'2025-01-07','17:18:00'),
(1,46,46,0,'2025-08-29','15:28:00'),
(1,47,47,0,'2025-06-24','10:03:00'),
(1,48,48,0,'2025-05-10','15:07:00'),
(1,49,49,0,'2025-02-03','14:31:00'),
(1,50, 1,0,'2025-02-07','17:40:00');

INSERT INTO stock_balance (pharmacy_id, product_id, remaining_qty) VALUES
(1,11,274),(1,20,138),(1,39,222),(1,73,109),
(2,29,193),(2,77,176),(2,78,211),(2,80,190),(2,100,156),
(3,8,133),(3,40,267),(3,73,100),(3,79,240),(3,80,272),
(4,11,218),(4,21,97),(4,34,139),(4,85,123),
(5,1,232),(5,53,195),(5,58,153),(5,89,84),
(6,37,93),(6,59,146),(6,90,135),(6,91,251),
(7,15,113),(7,29,288),(7,55,244),(7,70,147),
(8,8,267),(8,10,230),(8,22,286),(8,40,224),
(9,16,179),(9,39,214),(9,57,259),(9,60,146),(9,89,207),
(10,6,143),(10,11,162),(10,56,83),(10,57,264),(10,77,234),
(11,30,81),(11,74,248),(11,76,287),(11,87,272),
(12,6,211),(12,23,147),(12,74,195),(12,97,193),(12,98,246),
(13,56,200),(13,63,179),(13,75,98),(13,82,165),
(14,14,203),(14,21,151),(14,42,184),(14,43,247),(14,86,253),
(15,5,162),(15,12,273),(15,59,106),(15,71,141),(15,98,160),
(16,1,89),(16,60,168),(16,66,184),(16,70,208),(16,85,123),
(17,7,153),(17,27,187),(17,57,219),(17,81,143),(17,98,112),
(18,4,120),(18,16,259),(18,31,80),(18,78,218),(18,81,158),
(19,12,242),(19,15,119),(19,16,259),(19,29,288),(19,60,202),
(20,35,196),(20,54,216),(20,62,113),(20,66,195),(20,91,141),
(21,18,297),(21,25,97),(21,66,273),(21,77,149),(21,96,282),
(22,1,227),(22,35,291),(22,37,224),(22,44,264),(22,65,156),
(23,20,163),(23,45,175),(23,58,219),(23,62,217),(23,69,270),
(24,25,136),(24,31,275),(24,42,173),(24,74,181),(24,90,295),
(25,41,286),(25,61,177),(25,91,244),(25,96,174),
(26,5,161),(26,17,103),(26,64,227),(26,65,291),
(27,2,115),(27,13,111),(27,59,246),(27,68,184),(27,93,94),
(28,34,241),(28,44,96),(28,51,295),(28,80,293),(28,89,161),
(29,41,213),(29,63,140),(29,81,86),(29,92,233),(29,98,93),
(30,12,255),(30,13,191),(30,30,274),(30,56,104),(30,96,241),
(31,4,93),(31,6,154),(31,39,281),(31,89,161),
(32,19,252),(32,32,281),(32,48,183),(32,56,222),(32,68,121),
(33,11,252),(33,23,235),(33,49,205),(33,79,137),
(34,30,193),(34,33,80),(34,60,140),(34,82,247),
(35,10,238),(35,21,154),(35,37,190),(35,70,227),(35,87,164),
(36,26,140),(36,33,295),(36,39,106),(36,59,201),(36,89,176),
(37,3,81),(37,38,177),(37,46,244),(37,74,287),(37,90,147),
(38,37,233),(38,64,138),(38,78,277),(38,96,282),
(39,25,262),(39,29,248),(39,33,243),(39,80,275),(39,82,269),
(40,6,83),(40,13,276),(40,81,156),(40,83,191),
(41,12,119),(41,17,185),(41,38,130),(41,42,111),(41,94,268),
(42,22,153),(42,33,271),(42,35,282),(42,65,203),(42,68,288),
(43,10,253),(43,15,135),(43,19,263),(43,60,297),(43,97,252),
(44,2,173),(44,12,107),(44,47,214),(44,51,193),(44,72,144),
(45,14,234),(45,48,82),(45,49,136),(45,75,252),(45,82,197),
(46,9,156),(46,29,195),(46,79,290),(46,82,245),(46,83,256),
(47,5,136),(47,6,100),(47,15,202),(47,18,106),(47,39,216),
(48,48,215),(48,50,269),(48,59,257),(48,86,183),
(49,13,147),(49,54,237),(49,63,83),(49,84,181),
(50,13,166),(50,28,252),(50,31,218),(50,47,243),(50,57,169);

INSERT INTO order_item (receipt_id, product_id, quantity, unit_price, discount) VALUES
(1,11,1,(SELECT sale_price FROM product WHERE product_id=11),5),
(1,73,1,(SELECT sale_price FROM product WHERE product_id=73),0),
(1,20,5,(SELECT sale_price FROM product WHERE product_id=20),0),
(2,80,5,(SELECT sale_price FROM product WHERE product_id=80),0),
(2,77,1,(SELECT sale_price FROM product WHERE product_id=77),10),
(2,78,2,(SELECT sale_price FROM product WHERE product_id=78),10),
(3,73,5,(SELECT sale_price FROM product WHERE product_id=73),0),
(3,80,3,(SELECT sale_price FROM product WHERE product_id=80),0),
(3,40,2,(SELECT sale_price FROM product WHERE product_id=40),10),
(4,85,1,(SELECT sale_price FROM product WHERE product_id=85),0),
(4,34,2,(SELECT sale_price FROM product WHERE product_id=34),10),
(4,11,3,(SELECT sale_price FROM product WHERE product_id=11),0),
(4,21,2,(SELECT sale_price FROM product WHERE product_id=21),0),
(5,53,5,(SELECT sale_price FROM product WHERE product_id=53),0),
(5,58,1,(SELECT sale_price FROM product WHERE product_id=58),5),
(5,89,4,(SELECT sale_price FROM product WHERE product_id=89),0),
(6,37,5,(SELECT sale_price FROM product WHERE product_id=37),0),
(6,91,4,(SELECT sale_price FROM product WHERE product_id=91),0),
(6,59,1,(SELECT sale_price FROM product WHERE product_id=59),5),
(6,90,4,(SELECT sale_price FROM product WHERE product_id=90),5),
(7,55,1,(SELECT sale_price FROM product WHERE product_id=55),0),
(7,15,5,(SELECT sale_price FROM product WHERE product_id=15),0),
(7,70,1,(SELECT sale_price FROM product WHERE product_id=70),0),
(7,29,3,(SELECT sale_price FROM product WHERE product_id=29),10),
(8,22,5,(SELECT sale_price FROM product WHERE product_id=22),10),
(8,8,4,(SELECT sale_price FROM product WHERE product_id=8),0),
(8,40,1,(SELECT sale_price FROM product WHERE product_id=40),10),
(8,10,2,(SELECT sale_price FROM product WHERE product_id=10),5),
(9,16,4,(SELECT sale_price FROM product WHERE product_id=16),5),
(9,89,1,(SELECT sale_price FROM product WHERE product_id=89),0),
(9,39,4,(SELECT sale_price FROM product WHERE product_id=39),0),
(9,60,3,(SELECT sale_price FROM product WHERE product_id=60),0),
(10,6,1,(SELECT sale_price FROM product WHERE product_id=6),0),
(10,57,4,(SELECT sale_price FROM product WHERE product_id=57),0),
(10,56,3,(SELECT sale_price FROM product WHERE product_id=56),0),
(11,30,4,(SELECT sale_price FROM product WHERE product_id=30),0),
(11,74,4,(SELECT sale_price FROM product WHERE product_id=74),0),
(11,87,3,(SELECT sale_price FROM product WHERE product_id=87),10),
(11,76,3,(SELECT sale_price FROM product WHERE product_id=76),0),
(12,23,4,(SELECT sale_price FROM product WHERE product_id=23),0),
(12,6,1,(SELECT sale_price FROM product WHERE product_id=6),0),
(12,74,5,(SELECT sale_price FROM product WHERE product_id=74),0),
(13,82,4,(SELECT sale_price FROM product WHERE product_id=82),10),
(13,63,5,(SELECT sale_price FROM product WHERE product_id=63),10),
(13,75,5,(SELECT sale_price FROM product WHERE product_id=75),0),
(14,14,3,(SELECT sale_price FROM product WHERE product_id=14),0),
(14,42,1,(SELECT sale_price FROM product WHERE product_id=42),0),
(14,21,2,(SELECT sale_price FROM product WHERE product_id=21),10),
(14,86,4,(SELECT sale_price FROM product WHERE product_id=86),0),
(14,43,2,(SELECT sale_price FROM product WHERE product_id=43),0),
(15,12,4,(SELECT sale_price FROM product WHERE product_id=12),5),
(15,71,3,(SELECT sale_price FROM product WHERE product_id=71),0),
(15,59,3,(SELECT sale_price FROM product WHERE product_id=59),0),
(16,85,5,(SELECT sale_price FROM product WHERE product_id=85),0),
(16,66,1,(SELECT sale_price FROM product WHERE product_id=66),0),
(16,70,4,(SELECT sale_price FROM product WHERE product_id=70),5),
(16,60,4,(SELECT sale_price FROM product WHERE product_id=60),10),
(16,1,4,(SELECT sale_price FROM product WHERE product_id=1),5),
(17,57,1,(SELECT sale_price FROM product WHERE product_id=57),0),
(17,81,5,(SELECT sale_price FROM product WHERE product_id=81),5),
(17,27,5,(SELECT sale_price FROM product WHERE product_id=27),0),
(17,98,1,(SELECT sale_price FROM product WHERE product_id=98),0),
(18,31,3,(SELECT sale_price FROM product WHERE product_id=31),0),
(18,16,2,(SELECT sale_price FROM product WHERE product_id=16),0),
(18,78,3,(SELECT sale_price FROM product WHERE product_id=78),0),
(18,81,1,(SELECT sale_price FROM product WHERE product_id=81),0),
(19,60,5,(SELECT sale_price FROM product WHERE product_id=60),0),
(19,29,5,(SELECT sale_price FROM product WHERE product_id=29),10),
(19,12,3,(SELECT sale_price FROM product WHERE product_id=12),0),
(19,16,4,(SELECT sale_price FROM product WHERE product_id=16),0),
(20,66,5,(SELECT sale_price FROM product WHERE product_id=66),5),
(20,54,5,(SELECT sale_price FROM product WHERE product_id=54),5),
(20,62,4,(SELECT sale_price FROM product WHERE product_id=62),0),
(20,91,1,(SELECT sale_price FROM product WHERE product_id=91),5),
(21,66,4,(SELECT sale_price FROM product WHERE product_id=66),0),
(21,77,1,(SELECT sale_price FROM product WHERE product_id=77),5),
(21,18,1,(SELECT sale_price FROM product WHERE product_id=18),0),
(22,37,4,(SELECT sale_price FROM product WHERE product_id=37),0),
(22,1,3,(SELECT sale_price FROM product WHERE product_id=1),0),
(22,35,3,(SELECT sale_price FROM product WHERE product_id=35),0),
(22,44,1,(SELECT sale_price FROM product WHERE product_id=44),10),
(23,20,2,(SELECT sale_price FROM product WHERE product_id=20),0),
(23,58,2,(SELECT sale_price FROM product WHERE product_id=58),0),
(23,62,2,(SELECT sale_price FROM product WHERE product_id=62),0),
(23,69,5,(SELECT sale_price FROM product WHERE product_id=69),0),
(23,45,1,(SELECT sale_price FROM product WHERE product_id=45),0),
(24,31,3,(SELECT sale_price FROM product WHERE product_id=31),0),
(24,74,4,(SELECT sale_price FROM product WHERE product_id=74),0),
(24,90,4,(SELECT sale_price FROM product WHERE product_id=90),5),
(24,25,3,(SELECT sale_price FROM product WHERE product_id=25),0),
(24,42,5,(SELECT sale_price FROM product WHERE product_id=42),0),
(25,41,1,(SELECT sale_price FROM product WHERE product_id=41),0),
(25,96,3,(SELECT sale_price FROM product WHERE product_id=96),0),
(25,61,1,(SELECT sale_price FROM product WHERE product_id=61),0),
(25,91,5,(SELECT sale_price FROM product WHERE product_id=91),10),
(26,65,5,(SELECT sale_price FROM product WHERE product_id=65),0),
(26,5,3,(SELECT sale_price FROM product WHERE product_id=5),10),
(26,64,4,(SELECT sale_price FROM product WHERE product_id=64),10),
(26,17,2,(SELECT sale_price FROM product WHERE product_id=17),0),
(27,13,5,(SELECT sale_price FROM product WHERE product_id=13),0),
(27,59,1,(SELECT sale_price FROM product WHERE product_id=59),0),
(27,2,4,(SELECT sale_price FROM product WHERE product_id=2),5),
(27,93,5,(SELECT sale_price FROM product WHERE product_id=93),10),
(28,44,4,(SELECT sale_price FROM product WHERE product_id=44),0),
(28,80,5,(SELECT sale_price FROM product WHERE product_id=80),0),
(28,51,3,(SELECT sale_price FROM product WHERE product_id=51),0),
(28,89,3,(SELECT sale_price FROM product WHERE product_id=89),0),
(28,34,5,(SELECT sale_price FROM product WHERE product_id=34),5),
(29,92,5,(SELECT sale_price FROM product WHERE product_id=92),0),
(29,81,3,(SELECT sale_price FROM product WHERE product_id=81),0),
(29,98,4,(SELECT sale_price FROM product WHERE product_id=98),0),
(29,41,5,(SELECT sale_price FROM product WHERE product_id=41),0),
(30,13,2,(SELECT sale_price FROM product WHERE product_id=13),10),
(30,56,1,(SELECT sale_price FROM product WHERE product_id=56),5),
(30,12,5,(SELECT sale_price FROM product WHERE product_id=12),0),
(30,96,1,(SELECT sale_price FROM product WHERE product_id=96),0),
(31,39,2,(SELECT sale_price FROM product WHERE product_id=39),0),
(31,89,2,(SELECT sale_price FROM product WHERE product_id=89),0),
(31,6,1,(SELECT sale_price FROM product WHERE product_id=6),0),
(31,4,1,(SELECT sale_price FROM product WHERE product_id=4),10),
(32,68,5,(SELECT sale_price FROM product WHERE product_id=68),5),
(32,48,2,(SELECT sale_price FROM product WHERE product_id=48),0),
(32,19,2,(SELECT sale_price FROM product WHERE product_id=19),10),
(32,56,2,(SELECT sale_price FROM product WHERE product_id=56),5),
(32,32,1,(SELECT sale_price FROM product WHERE product_id=32),5),
(33,11,2,(SELECT sale_price FROM product WHERE product_id=11),10),
(33,49,2,(SELECT sale_price FROM product WHERE product_id=49),0),
(33,79,4,(SELECT sale_price FROM product WHERE product_id=79),0),
(33,23,3,(SELECT sale_price FROM product WHERE product_id=23),10),
(34,33,2,(SELECT sale_price FROM product WHERE product_id=33),0),
(34,60,5,(SELECT sale_price FROM product WHERE product_id=60),0),
(34,82,3,(SELECT sale_price FROM product WHERE product_id=82),0),
(34,30,4,(SELECT sale_price FROM product WHERE product_id=30),0),
(35,37,3,(SELECT sale_price FROM product WHERE product_id=37),5),
(35,10,5,(SELECT sale_price FROM product WHERE product_id=10),0),
(35,87,4,(SELECT sale_price FROM product WHERE product_id=87),0),
(35,21,2,(SELECT sale_price FROM product WHERE product_id=21),5),
(35,70,3,(SELECT sale_price FROM product WHERE product_id=70),0),
(36,89,2,(SELECT sale_price FROM product WHERE product_id=89),5),
(36,39,1,(SELECT sale_price FROM product WHERE product_id=39),0),
(36,59,2,(SELECT sale_price FROM product WHERE product_id=59),0),
(36,33,3,(SELECT sale_price FROM product WHERE product_id=33),0),
(37,38,4,(SELECT sale_price FROM product WHERE product_id=38),0),
(37,46,4,(SELECT sale_price FROM product WHERE product_id=46),5),
(37,74,5,(SELECT sale_price FROM product WHERE product_id=74),0),
(37,3,1,(SELECT sale_price FROM product WHERE product_id=3),0),
(37,90,3,(SELECT sale_price FROM product WHERE product_id=90),0),
(38,37,2,(SELECT sale_price FROM product WHERE product_id=37),0),
(38,78,1,(SELECT sale_price FROM product WHERE product_id=78),0),
(38,96,2,(SELECT sale_price FROM product WHERE product_id=96),5),
(39,33,5,(SELECT sale_price FROM product WHERE product_id=33),10),
(39,80,1,(SELECT sale_price FROM product WHERE product_id=80),10),
(39,29,5,(SELECT sale_price FROM product WHERE product_id=29),0),
(39,25,2,(SELECT sale_price FROM product WHERE product_id=25),0),
(39,82,4,(SELECT sale_price FROM product WHERE product_id=82),0),
(40,83,1,(SELECT sale_price FROM product WHERE product_id=83),10),
(40,81,3,(SELECT sale_price FROM product WHERE product_id=81),0),
(40,13,5,(SELECT sale_price FROM product WHERE product_id=13),10),
(40,6,5,(SELECT sale_price FROM product WHERE product_id=6),10),
(41,94,3,(SELECT sale_price FROM product WHERE product_id=94),0),
(41,38,1,(SELECT sale_price FROM product WHERE product_id=38),0),
(41,17,1,(SELECT sale_price FROM product WHERE product_id=17),0),
(41,42,2,(SELECT sale_price FROM product WHERE product_id=42),0),
(41,12,5,(SELECT sale_price FROM product WHERE product_id=12),0),
(42,68,3,(SELECT sale_price FROM product WHERE product_id=68),0),
(42,35,4,(SELECT sale_price FROM product WHERE product_id=35),5),
(42,22,2,(SELECT sale_price FROM product WHERE product_id=22),0),
(43,19,2,(SELECT sale_price FROM product WHERE product_id=19),0),
(43,15,2,(SELECT sale_price FROM product WHERE product_id=15),0),
(43,60,3,(SELECT sale_price FROM product WHERE product_id=60),0),
(44,51,3,(SELECT sale_price FROM product WHERE product_id=51),0),
(44,2,1,(SELECT sale_price FROM product WHERE product_id=2),0),
(44,47,3,(SELECT sale_price FROM product WHERE product_id=47),0),
(44,72,3,(SELECT sale_price FROM product WHERE product_id=72),5),
(44,12,4,(SELECT sale_price FROM product WHERE product_id=12),5),
(45,82,3,(SELECT sale_price FROM product WHERE product_id=82),10),
(45,49,3,(SELECT sale_price FROM product WHERE product_id=49),0),
(45,48,4,(SELECT sale_price FROM product WHERE product_id=48),0),
(45,14,4,(SELECT sale_price FROM product WHERE product_id=14),10),
(46,82,1,(SELECT sale_price FROM product WHERE product_id=82),0),
(46,83,3,(SELECT sale_price FROM product WHERE product_id=83),0),
(46,29,3,(SELECT sale_price FROM product WHERE product_id=29),5),
(46,9,1,(SELECT sale_price FROM product WHERE product_id=9),5),
(47,5,4,(SELECT sale_price FROM product WHERE product_id=5),0),
(47,18,3,(SELECT sale_price FROM product WHERE product_id=18),10),
(47,6,4,(SELECT sale_price FROM product WHERE product_id=6),0),
(47,39,1,(SELECT sale_price FROM product WHERE product_id=39),0),
(47,15,4,(SELECT sale_price FROM product WHERE product_id=15),0),
(48,59,1,(SELECT sale_price FROM product WHERE product_id=59),0),
(48,50,1,(SELECT sale_price FROM product WHERE product_id=50),0),
(48,48,3,(SELECT sale_price FROM product WHERE product_id=48),0),
(48,86,4,(SELECT sale_price FROM product WHERE product_id=86),10),
(49,84,3,(SELECT sale_price FROM product WHERE product_id=84),0),
(49,13,4,(SELECT sale_price FROM product WHERE product_id=13),0),
(49,63,5,(SELECT sale_price FROM product WHERE product_id=63),0),
(50,31,1,(SELECT sale_price FROM product WHERE product_id=31),0),
(50,47,2,(SELECT sale_price FROM product WHERE product_id=47),5),
(50,57,5,(SELECT sale_price FROM product WHERE product_id=57),5),
(50,13,5,(SELECT sale_price FROM product WHERE product_id=13),0),
(50,28,3,(SELECT sale_price FROM product WHERE product_id=28),0);

UPDATE receipt r
SET total_amount = (
    SELECT COALESCE(SUM(total_price), 0)
    FROM order_item WHERE receipt_id = r.receipt_id
);

-- ══════════════════════════════════════════════════════════════════
-- ХРАНИМЫЕ ПРОЦЕДУРЫ
-- ══════════════════════════════════════════════════════════════════

-- ── CATEGORY ─────────────────────────────────────────────────────

CREATE OR REPLACE PROCEDURE sp_add_category(
    p_name VARCHAR, p_description TEXT, OUT p_id INT
)
LANGUAGE plpgsql AS $$
BEGIN
    INSERT INTO category(name, description) VALUES (p_name, p_description)
    RETURNING category_id INTO p_id;
END; $$;

CREATE OR REPLACE PROCEDURE sp_update_category(
    p_id INT, p_name VARCHAR, p_description TEXT
)
LANGUAGE plpgsql AS $$
BEGIN
    UPDATE category SET name=p_name, description=p_description
    WHERE category_id = p_id;
    IF NOT FOUND THEN RAISE EXCEPTION 'Категория с id=% не найдена', p_id; END IF;
END; $$;

CREATE OR REPLACE PROCEDURE sp_delete_category(p_id INT)
LANGUAGE plpgsql AS $$
BEGIN
    DELETE FROM category WHERE category_id = p_id;
    IF NOT FOUND THEN RAISE EXCEPTION 'Категория с id=% не найдена', p_id; END IF;
END; $$;

-- ── ROLE ─────────────────────────────────────────────────────────

CREATE OR REPLACE PROCEDURE sp_add_role(
    p_name VARCHAR, p_fixed_salary NUMERIC, p_description TEXT, OUT p_id INT
)
LANGUAGE plpgsql AS $$
BEGIN
    INSERT INTO role(name, fixed_salary, description) VALUES (p_name, p_fixed_salary, p_description)
    RETURNING role_id INTO p_id;
END; $$;

CREATE OR REPLACE PROCEDURE sp_update_role(
    p_id INT, p_name VARCHAR, p_fixed_salary NUMERIC, p_description TEXT
)
LANGUAGE plpgsql AS $$
BEGIN
    UPDATE role SET name=p_name, fixed_salary=p_fixed_salary, description=p_description
    WHERE role_id = p_id;
    IF NOT FOUND THEN RAISE EXCEPTION 'Роль с id=% не найдена', p_id; END IF;
END; $$;

CREATE OR REPLACE PROCEDURE sp_delete_role(p_id INT)
LANGUAGE plpgsql AS $$
BEGIN
    DELETE FROM role WHERE role_id = p_id;
    IF NOT FOUND THEN RAISE EXCEPTION 'Роль с id=% не найдена', p_id; END IF;
END; $$;

-- ── MANUFACTURER ─────────────────────────────────────────────────

CREATE OR REPLACE PROCEDURE sp_add_manufacturer(
    p_name VARCHAR, p_country VARCHAR, p_address VARCHAR,
    p_phone VARCHAR, p_email VARCHAR, OUT p_id INT
)
LANGUAGE plpgsql AS $$
BEGIN
    INSERT INTO manufacturer(name, country, address, phone, email)
    VALUES (p_name, p_country, p_address, p_phone, p_email)
    RETURNING manufacturer_id INTO p_id;
END; $$;

CREATE OR REPLACE PROCEDURE sp_update_manufacturer(
    p_id INT, p_name VARCHAR, p_country VARCHAR,
    p_address VARCHAR, p_phone VARCHAR, p_email VARCHAR
)
LANGUAGE plpgsql AS $$
BEGIN
    UPDATE manufacturer
    SET name=p_name, country=p_country, address=p_address, phone=p_phone, email=p_email
    WHERE manufacturer_id = p_id;
    IF NOT FOUND THEN RAISE EXCEPTION 'Производитель с id=% не найден', p_id; END IF;
END; $$;

CREATE OR REPLACE PROCEDURE sp_delete_manufacturer(p_id INT)
LANGUAGE plpgsql AS $$
BEGIN
    DELETE FROM manufacturer WHERE manufacturer_id = p_id;
    IF NOT FOUND THEN RAISE EXCEPTION 'Производитель с id=% не найден', p_id; END IF;
END; $$;

-- ── PRODUCT ──────────────────────────────────────────────────────

CREATE OR REPLACE PROCEDURE sp_add_product(
    p_name VARCHAR, p_category_id INT, p_manufacturer_id INT,
    p_production_date DATE, p_expiration_date DATE,
    p_unit VARCHAR, p_description TEXT, p_prescription_required BOOLEAN,
    p_purchase_price NUMERIC, p_sale_price NUMERIC, p_file_path TEXT, OUT p_id INT
)
LANGUAGE plpgsql AS $$
BEGIN
    INSERT INTO product(name, category_id, manufacturer_id, production_date, expiration_date,
                        unit, description, prescription_required, purchase_price, sale_price, file_path)
    VALUES (p_name, p_category_id, p_manufacturer_id, p_production_date, p_expiration_date,
            p_unit, p_description, p_prescription_required, p_purchase_price, p_sale_price, p_file_path)
    RETURNING product_id INTO p_id;
END; $$;

CREATE OR REPLACE PROCEDURE sp_update_product(
    p_id INT, p_name VARCHAR, p_category_id INT, p_manufacturer_id INT,
    p_production_date DATE, p_expiration_date DATE,
    p_unit VARCHAR, p_description TEXT, p_prescription_required BOOLEAN,
    p_purchase_price NUMERIC, p_sale_price NUMERIC, p_file_path TEXT
)
LANGUAGE plpgsql AS $$
BEGIN
    UPDATE product
    SET name=p_name, category_id=p_category_id, manufacturer_id=p_manufacturer_id,
        production_date=p_production_date, expiration_date=p_expiration_date,
        unit=p_unit, description=p_description,
        prescription_required=p_prescription_required,
        purchase_price=p_purchase_price, sale_price=p_sale_price,
        file_path=p_file_path
    WHERE product_id = p_id;
    IF NOT FOUND THEN RAISE EXCEPTION 'Товар с id=% не найден', p_id; END IF;
END; $$;

CREATE OR REPLACE PROCEDURE sp_delete_product(p_id INT)
LANGUAGE plpgsql AS $$
BEGIN
    DELETE FROM product WHERE product_id = p_id;
    IF NOT FOUND THEN RAISE EXCEPTION 'Товар с id=% не найден', p_id; END IF;
END; $$;

-- ── PHARMACY ─────────────────────────────────────────────────────

CREATE OR REPLACE PROCEDURE sp_add_pharmacy(
    p_address VARCHAR, p_phone VARCHAR, p_working_hours VARCHAR, OUT p_id INT
)
LANGUAGE plpgsql AS $$
BEGIN
    INSERT INTO pharmacy(address, phone, working_hours) VALUES (p_address, p_phone, p_working_hours)
    RETURNING pharmacy_id INTO p_id;
END; $$;

CREATE OR REPLACE PROCEDURE sp_update_pharmacy(
    p_id INT, p_address VARCHAR, p_phone VARCHAR, p_working_hours VARCHAR
)
LANGUAGE plpgsql AS $$
BEGIN
    UPDATE pharmacy SET address=p_address, phone=p_phone, working_hours=p_working_hours
    WHERE pharmacy_id = p_id;
    IF NOT FOUND THEN RAISE EXCEPTION 'Аптека с id=% не найдена', p_id; END IF;
END; $$;

CREATE OR REPLACE PROCEDURE sp_delete_pharmacy(p_id INT)
LANGUAGE plpgsql AS $$
BEGIN
    DELETE FROM pharmacy WHERE pharmacy_id = p_id;
    IF NOT FOUND THEN RAISE EXCEPTION 'Аптека с id=% не найдена', p_id; END IF;
END; $$;

-- ── EMPLOYEE ─────────────────────────────────────────────────────

CREATE OR REPLACE PROCEDURE sp_add_employee(
    p_full_name VARCHAR, p_idnp VARCHAR, p_phone VARCHAR,
    p_address VARCHAR, p_role_id INT, OUT p_id INT
)
LANGUAGE plpgsql AS $$
BEGIN
    INSERT INTO employee(full_name, idnp, phone, address, role_id)
    VALUES (p_full_name, p_idnp, p_phone, p_address, p_role_id)
    RETURNING employee_id INTO p_id;
END; $$;

CREATE OR REPLACE PROCEDURE sp_update_employee(
    p_id INT, p_full_name VARCHAR, p_idnp VARCHAR, p_phone VARCHAR,
    p_address VARCHAR, p_role_id INT
)
LANGUAGE plpgsql AS $$
BEGIN
    UPDATE employee
    SET full_name=p_full_name, idnp=p_idnp, phone=p_phone,
        address=p_address, role_id=p_role_id
    WHERE employee_id = p_id;
    IF NOT FOUND THEN RAISE EXCEPTION 'Сотрудник с id=% не найден', p_id; END IF;
END; $$;

CREATE OR REPLACE PROCEDURE sp_delete_employee(p_id INT)
LANGUAGE plpgsql AS $$
BEGIN
    DELETE FROM employee WHERE employee_id = p_id;
    IF NOT FOUND THEN RAISE EXCEPTION 'Сотрудник с id=% не найден', p_id; END IF;
END; $$;

-- ── RECEIPT ──────────────────────────────────────────────────────

CREATE OR REPLACE PROCEDURE sp_add_receipt(
    p_pharmacy_id INT, p_employee_id INT, p_date DATE, OUT p_id INT
)
LANGUAGE plpgsql AS $$
DECLARE v_number INT;
BEGIN
    SELECT COALESCE(MAX(receipt_number), 0) + 1 INTO v_number
    FROM receipt WHERE pharmacy_id = p_pharmacy_id AND date = p_date;

    INSERT INTO receipt(receipt_number, pharmacy_id, employee_id, total_amount, date, time)
    VALUES (v_number, p_pharmacy_id, p_employee_id, 0, p_date, CURRENT_TIME)
    RETURNING receipt_id INTO p_id;
END; $$;

CREATE OR REPLACE PROCEDURE sp_update_receipt(
    p_id INT, p_pharmacy_id INT, p_employee_id INT, p_date DATE
)
LANGUAGE plpgsql AS $$
DECLARE
    v_number          INT;
    v_old_pharmacy_id INT;
    v_old_date        DATE;
BEGIN
    SELECT pharmacy_id, date INTO v_old_pharmacy_id, v_old_date
    FROM receipt WHERE receipt_id = p_id;

    IF v_old_pharmacy_id != p_pharmacy_id OR v_old_date != p_date THEN
        SELECT COALESCE(MAX(receipt_number), 0) + 1 INTO v_number
        FROM receipt WHERE pharmacy_id = p_pharmacy_id AND date = p_date AND receipt_id != p_id;
    ELSE
        SELECT receipt_number INTO v_number FROM receipt WHERE receipt_id = p_id;
    END IF;

    UPDATE receipt
    SET pharmacy_id=p_pharmacy_id, employee_id=p_employee_id,
        date=p_date, receipt_number=v_number
    WHERE receipt_id = p_id;
    IF NOT FOUND THEN RAISE EXCEPTION 'Чек с id=% не найден', p_id; END IF;
END; $$;

CREATE OR REPLACE PROCEDURE sp_delete_receipt(p_id INT)
LANGUAGE plpgsql AS $$
BEGIN
    DELETE FROM receipt WHERE receipt_id = p_id;
    IF NOT FOUND THEN RAISE EXCEPTION 'Чек с id=% не найден', p_id; END IF;
END; $$;

-- ── ORDER ITEM ───────────────────────────────────────────────────

CREATE OR REPLACE PROCEDURE sp_add_order_item(
    p_receipt_id INT, p_product_id INT, p_quantity INT, p_discount NUMERIC
)
LANGUAGE plpgsql AS $$
DECLARE
    v_pharmacy_id INT;
    v_stock       INT;
BEGIN
    SELECT pharmacy_id INTO v_pharmacy_id FROM receipt WHERE receipt_id = p_receipt_id;
    IF v_pharmacy_id IS NULL THEN RAISE EXCEPTION 'Чек с id=% не найден', p_receipt_id; END IF;

    SELECT remaining_qty INTO v_stock FROM stock_balance
    WHERE pharmacy_id = v_pharmacy_id AND product_id = p_product_id;

    IF v_stock IS NULL THEN RAISE EXCEPTION 'Товар отсутствует на складе этой аптеки'; END IF;
    IF v_stock < p_quantity THEN RAISE EXCEPTION 'Недостаточно товара. Доступно: %', v_stock; END IF;

    INSERT INTO order_item(receipt_id, product_id, quantity, discount)
    VALUES (p_receipt_id, p_product_id, p_quantity, p_discount)
    ON CONFLICT (receipt_id, product_id) DO UPDATE
    SET quantity = order_item.quantity + EXCLUDED.quantity, discount = EXCLUDED.discount;

    UPDATE stock_balance SET remaining_qty = remaining_qty - p_quantity
    WHERE pharmacy_id = v_pharmacy_id AND product_id = p_product_id;

    UPDATE receipt SET total_amount = (
        SELECT COALESCE(SUM(total_price), 0) FROM order_item WHERE receipt_id = p_receipt_id
    ) WHERE receipt_id = p_receipt_id;
END; $$;

CREATE OR REPLACE PROCEDURE sp_update_order_item(
    p_receipt_id INT, p_product_id INT,
    p_new_receipt_id INT, p_new_product_id INT,
    p_quantity INT, p_discount NUMERIC
)
LANGUAGE plpgsql AS $$
DECLARE
    v_old_pharmacy_id INT;
    v_new_pharmacy_id INT;
    v_old_qty         INT;
    v_stock           INT;
    v_price           NUMERIC;
    v_delta           INT;
BEGIN
    SELECT pharmacy_id INTO v_old_pharmacy_id FROM receipt WHERE receipt_id = p_receipt_id;
    SELECT pharmacy_id INTO v_new_pharmacy_id FROM receipt WHERE receipt_id = p_new_receipt_id;
    SELECT quantity INTO v_old_qty FROM order_item
    WHERE receipt_id = p_receipt_id AND product_id = p_product_id;

    IF p_receipt_id != p_new_receipt_id OR p_product_id != p_new_product_id THEN
        UPDATE stock_balance SET remaining_qty = remaining_qty + v_old_qty
        WHERE pharmacy_id = v_old_pharmacy_id AND product_id = p_product_id;

        SELECT remaining_qty INTO v_stock FROM stock_balance
        WHERE pharmacy_id = v_new_pharmacy_id AND product_id = p_new_product_id;

        IF v_stock IS NULL THEN
            UPDATE stock_balance SET remaining_qty = remaining_qty - v_old_qty
            WHERE pharmacy_id = v_old_pharmacy_id AND product_id = p_product_id;
            RAISE EXCEPTION 'Товар отсутствует на складе аптеки';
        END IF;
        IF v_stock < p_quantity THEN
            UPDATE stock_balance SET remaining_qty = remaining_qty - v_old_qty
            WHERE pharmacy_id = v_old_pharmacy_id AND product_id = p_product_id;
            RAISE EXCEPTION 'Недостаточно товара. Доступно: %', v_stock;
        END IF;

        DELETE FROM order_item WHERE receipt_id = p_receipt_id AND product_id = p_product_id;
        SELECT sale_price INTO v_price FROM product WHERE product_id = p_new_product_id;
        INSERT INTO order_item(receipt_id, product_id, quantity, unit_price, discount)
        VALUES (p_new_receipt_id, p_new_product_id, p_quantity, v_price, p_discount);
        UPDATE stock_balance SET remaining_qty = remaining_qty - p_quantity
        WHERE pharmacy_id = v_new_pharmacy_id AND product_id = p_new_product_id;
    ELSE
        v_delta := p_quantity - v_old_qty;
        IF v_delta > 0 THEN
            SELECT remaining_qty INTO v_stock FROM stock_balance
            WHERE pharmacy_id = v_old_pharmacy_id AND product_id = p_product_id;
            IF v_stock IS NULL OR v_stock < v_delta THEN
                RAISE EXCEPTION 'Недостаточно товара. Доступно: %', COALESCE(v_stock, 0);
            END IF;
            UPDATE stock_balance SET remaining_qty = remaining_qty - v_delta
            WHERE pharmacy_id = v_old_pharmacy_id AND product_id = p_product_id;
        ELSIF v_delta < 0 THEN
            UPDATE stock_balance SET remaining_qty = remaining_qty + ABS(v_delta)
            WHERE pharmacy_id = v_old_pharmacy_id AND product_id = p_product_id;
        END IF;
        UPDATE order_item SET quantity=p_quantity, discount=p_discount
        WHERE receipt_id = p_receipt_id AND product_id = p_product_id;
    END IF;

    UPDATE receipt SET total_amount = (
        SELECT COALESCE(SUM(total_price), 0) FROM order_item WHERE receipt_id = receipt.receipt_id
    ) WHERE receipt_id IN (p_receipt_id, p_new_receipt_id);
END; $$;

CREATE OR REPLACE PROCEDURE sp_delete_order_item(p_receipt_id INT, p_product_id INT)
LANGUAGE plpgsql AS $$
DECLARE
    v_pharmacy_id INT;
    v_qty         INT;
BEGIN
    SELECT pharmacy_id INTO v_pharmacy_id FROM receipt WHERE receipt_id = p_receipt_id;
    SELECT quantity INTO v_qty FROM order_item
    WHERE receipt_id = p_receipt_id AND product_id = p_product_id;
    IF NOT FOUND THEN
        RAISE EXCEPTION 'Позиция не найдена (receipt_id=%, product_id=%)', p_receipt_id, p_product_id;
    END IF;

    DELETE FROM order_item WHERE receipt_id = p_receipt_id AND product_id = p_product_id;
    UPDATE stock_balance SET remaining_qty = remaining_qty + v_qty
    WHERE pharmacy_id = v_pharmacy_id AND product_id = p_product_id;
    UPDATE receipt SET total_amount = (
        SELECT COALESCE(SUM(total_price), 0) FROM order_item WHERE receipt_id = p_receipt_id
    ) WHERE receipt_id = p_receipt_id;
END; $$;

-- ── STOCK BALANCE ────────────────────────────────────────────────

CREATE OR REPLACE PROCEDURE sp_add_stock_balance(
    p_pharmacy_id INT, p_product_id INT, p_remaining_qty INT
)
LANGUAGE plpgsql AS $$
BEGIN
    INSERT INTO stock_balance(pharmacy_id, product_id, remaining_qty)
    VALUES (p_pharmacy_id, p_product_id, p_remaining_qty);
END; $$;

CREATE OR REPLACE PROCEDURE sp_update_stock_balance(
    p_pharmacy_id INT, p_product_id INT,
    p_new_pharmacy_id INT, p_new_product_id INT, p_remaining_qty INT
)
LANGUAGE plpgsql AS $$
BEGIN
    IF p_pharmacy_id != p_new_pharmacy_id OR p_product_id != p_new_product_id THEN
        DELETE FROM stock_balance WHERE pharmacy_id=p_pharmacy_id AND product_id=p_product_id;
        INSERT INTO stock_balance(pharmacy_id, product_id, remaining_qty)
        VALUES (p_new_pharmacy_id, p_new_product_id, p_remaining_qty);
    ELSE
        UPDATE stock_balance SET remaining_qty=p_remaining_qty
        WHERE pharmacy_id=p_pharmacy_id AND product_id=p_product_id;
    END IF;
END; $$;

CREATE OR REPLACE PROCEDURE sp_delete_stock_balance(p_pharmacy_id INT, p_product_id INT)
LANGUAGE plpgsql AS $$
BEGIN
    DELETE FROM stock_balance WHERE pharmacy_id=p_pharmacy_id AND product_id=p_product_id;
    IF NOT FOUND THEN
        RAISE EXCEPTION 'Остаток не найден (pharmacy_id=%, product_id=%)', p_pharmacy_id, p_product_id;
    END IF;
END; $$;



-- ══════════════════════════════════════════════════════════════════
-- ХРАНИМЫЕ ПРОЦЕДУРЫ — SYSTEM USER
-- Добавить в конец SQL-скрипта (после остальных процедур)
-- ══════════════════════════════════════════════════════════════════

-- ── SYSTEM USER ──────────────────────────────────────────────────

-- system_role проставляется автоматически триггером trg_set_system_role,
-- поэтому в INSERT/UPDATE его не передаём.
-- Пароль хешируется функцией md5() на стороне PostgreSQL.

CREATE OR REPLACE PROCEDURE sp_add_system_users(
    p_employee_id INT,
    p_login       VARCHAR,
    p_password    VARCHAR,   -- plain-text, хешируется внутри процедуры
    p_is_active   BOOLEAN,
    OUT p_id      INT
)
LANGUAGE plpgsql AS $$
BEGIN
    -- Гарантируем: один сотрудник — один аккаунт
    IF EXISTS (
        SELECT 1 FROM system_users WHERE employee_id = p_employee_id
    ) THEN
        RAISE EXCEPTION 'У сотрудника с id=% уже есть системный аккаунт', p_employee_id;
    END IF;

    INSERT INTO system_users (employee_id, login, password_hash, is_active)
    VALUES (p_employee_id, p_login, md5(p_password), p_is_active)
    RETURNING user_id INTO p_id;
END; $$;

-- При обновлении пароль меняется только если p_password НЕ пустая строка.
CREATE OR REPLACE PROCEDURE sp_update_system_users(
    p_id          INT,
    p_employee_id INT,
    p_login       VARCHAR,
    p_password    VARCHAR,   -- '' = не менять пароль
    p_is_active   BOOLEAN
)
LANGUAGE plpgsql AS $$
BEGIN
    -- Проверяем уникальность employee_id среди других записей
    IF EXISTS (
        SELECT 1 FROM system_users
        WHERE employee_id = p_employee_id AND user_id <> p_id
    ) THEN
        RAISE EXCEPTION 'У сотрудника с id=% уже есть системный аккаунт', p_employee_id;
    END IF;

    IF p_password <> '' THEN
        UPDATE system_users
        SET employee_id   = p_employee_id,
            login         = p_login,
            password_hash = md5(p_password),
            is_active     = p_is_active
        WHERE user_id = p_id;
    ELSE
        UPDATE system_users
        SET employee_id = p_employee_id,
            login       = p_login,
            is_active   = p_is_active
        WHERE user_id = p_id;
    END IF;

    IF NOT FOUND THEN
        RAISE EXCEPTION 'Пользователь с id=% не найден', p_id;
    END IF;
END; $$;

CREATE OR REPLACE PROCEDURE sp_delete_system_users(p_id INT)
LANGUAGE plpgsql AS $$
BEGIN
    DELETE FROM system_users WHERE user_id = p_id;
    IF NOT FOUND THEN
        RAISE EXCEPTION 'Пользователь с id=% не найден', p_id;
    END IF;
END; $$;

-- ══════════════════════════════════════════════════════════════════
-- АНАЛИТИЧЕСКИЕ ПРЕДСТАВЛЕНИЯ
-- ══════════════════════════════════════════════════════════════════

CREATE OR REPLACE VIEW vw_revenue_by_month AS
SELECT TO_CHAR(date,'YYYY-MM') AS month, SUM(total_amount) AS total_revenue, COUNT(*) AS receipt_count
FROM receipt GROUP BY TO_CHAR(date,'YYYY-MM') ORDER BY month;

CREATE OR REPLACE VIEW vw_revenue_by_pharmacy AS
SELECT ph.address, SUM(r.total_amount) AS total_revenue,
       COUNT(r.receipt_id) AS receipt_count, AVG(r.total_amount) AS avg_receipt
FROM receipt r JOIN pharmacy ph ON ph.pharmacy_id = r.pharmacy_id
GROUP BY ph.pharmacy_id, ph.address ORDER BY total_revenue DESC;

CREATE OR REPLACE VIEW vw_top_products AS
SELECT p.name, c.name AS category,
       SUM(oi.quantity) AS total_qty, SUM(oi.total_price) AS total_revenue, AVG(oi.discount) AS avg_discount
FROM order_item oi
JOIN product  p ON p.product_id  = oi.product_id
JOIN category c ON c.category_id = p.category_id
GROUP BY p.product_id, p.name, c.name ORDER BY total_revenue DESC;

CREATE OR REPLACE VIEW vw_sales_by_category AS
SELECT c.name AS category, SUM(oi.quantity) AS total_qty, SUM(oi.total_price) AS total_revenue
FROM order_item oi
JOIN product  p ON p.product_id  = oi.product_id
JOIN category c ON c.category_id = p.category_id
GROUP BY c.category_id, c.name ORDER BY total_revenue DESC;

CREATE OR REPLACE VIEW vw_sales_by_prescription AS
SELECT CASE WHEN p.prescription_required THEN 'По рецепту' ELSE 'Без рецепта' END AS category,
       SUM(oi.quantity) AS total_qty, SUM(oi.total_price) AS total_revenue
FROM order_item oi JOIN product p ON p.product_id = oi.product_id
GROUP BY p.prescription_required;

CREATE OR REPLACE VIEW vw_low_stock AS
SELECT ph.address AS pharmacy, p.name AS product, c.name AS category, sb.remaining_qty
FROM stock_balance sb
JOIN pharmacy ph ON ph.pharmacy_id = sb.pharmacy_id
JOIN product  p  ON p.product_id   = sb.product_id
JOIN category c  ON c.category_id  = p.category_id
WHERE sb.remaining_qty < 20 ORDER BY sb.remaining_qty;
