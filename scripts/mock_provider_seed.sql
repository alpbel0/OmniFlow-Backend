-- OmniFlow mock provider + places seed
-- Safe to run multiple times.
--
-- Scope:
--   1. provider_flights: rolling 90 days, all directed routes between
--      Paris, Rome, Istanbul, Florence, Amsterdam, 3 flights per route/day.
--   2. provider_hotels: rolling 90 days, 10 hotels per city/day across
--      economy, standard, and premium prices.
--   3. places: 100 curated mock places, 20 per city.
--
-- Notes:
--   - Provider rows are owned by provider_name = 'OmniMock'. The script deletes
--     only OmniMock provider rows before reseeding the current rolling window.
--   - Places use deterministic UUIDs and ON CONFLICT(id) DO UPDATE.
--   - All timestamps are timestamp without time zone to match current schema.

BEGIN;

DELETE FROM provider_flights
WHERE provider_name = 'OmniMock';

DELETE FROM provider_hotels
WHERE provider_name = 'OmniMock';

WITH city_airports AS (
    SELECT *
    FROM (VALUES
        ('Paris', 'France', 'CDG'),
        ('Rome', 'Italy', 'FCO'),
        ('Istanbul', 'Turkey', 'IST'),
        ('Florence', 'Italy', 'FLR'),
        ('Amsterdam', 'Netherlands', 'AMS')
    ) AS c(city, country, airport_code)
),
route_base AS (
    SELECT * FROM (VALUES
        ('Paris', 'Rome', 135, 149.00),
        ('Paris', 'Istanbul', 220, 219.00),
        ('Paris', 'Florence', 110, 119.00),
        ('Paris', 'Amsterdam', 80, 89.00),
        ('Rome', 'Istanbul', 165, 179.00),
        ('Rome', 'Florence', 55, 79.00),
        ('Rome', 'Amsterdam', 155, 169.00),
        ('Istanbul', 'Florence', 165, 189.00),
        ('Istanbul', 'Amsterdam', 225, 229.00),
        ('Florence', 'Amsterdam', 145, 159.00)
    ) AS r(from_city, to_city, duration_minutes, base_price)
),
routes AS (
    SELECT from_city, to_city, duration_minutes, base_price FROM route_base
    UNION ALL
    SELECT to_city, from_city, duration_minutes, base_price FROM route_base
),
flight_slots AS (
    SELECT * FROM (VALUES
        (1, TIME '06:45', 0.00, 'Morning'),
        (2, TIME '13:20', 18.00, 'Midday'),
        (3, TIME '20:10', 32.00, 'Evening')
    ) AS s(slot_no, departure_time, price_delta, label)
),
flight_rows AS (
    SELECT
        (
            substr(md5('flight|' || r.from_city || '|' || r.to_city || '|' || d.day_offset || '|' || s.slot_no), 1, 8) || '-' ||
            substr(md5('flight|' || r.from_city || '|' || r.to_city || '|' || d.day_offset || '|' || s.slot_no), 9, 4) || '-' ||
            substr(md5('flight|' || r.from_city || '|' || r.to_city || '|' || d.day_offset || '|' || s.slot_no), 13, 4) || '-' ||
            substr(md5('flight|' || r.from_city || '|' || r.to_city || '|' || d.day_offset || '|' || s.slot_no), 17, 4) || '-' ||
            substr(md5('flight|' || r.from_city || '|' || r.to_city || '|' || d.day_offset || '|' || s.slot_no), 21, 12)
        )::uuid AS id,
        'OM' ||
            lpad((1000 + dense_rank() OVER (ORDER BY r.from_city, r.to_city) * 10 + s.slot_no)::text, 4, '0') AS flight_number,
        CASE (dense_rank() OVER (ORDER BY r.from_city, r.to_city) + s.slot_no) % 5
            WHEN 0 THEN 'Omni Air'
            WHEN 1 THEN 'SkyBridge Europe'
            WHEN 2 THEN 'Blue Atlas'
            WHEN 3 THEN 'Aero Vista'
            ELSE 'Sunline Connect'
        END AS airline,
        NULL::text AS airline_logo_url,
        r.from_city AS departure_city,
        r.to_city AS arrival_city,
        dep.airport_code AS departure_airport_code,
        arr.airport_code AS arrival_airport_code,
        ((CURRENT_DATE + d.day_offset)::timestamp + s.departure_time) AS departure_time,
        ((CURRENT_DATE + d.day_offset)::timestamp + s.departure_time + (r.duration_minutes || ' minutes')::interval) AS arrival_time,
        r.duration_minutes,
        (r.base_price + s.price_delta + ((d.day_offset % 14) * 2.50))::numeric(18,2) AS price,
        'EUR' AS currency_code,
        (30 + ((d.day_offset + s.slot_no) % 90))::integer AS available_seats,
        'OmniMock' AS provider_name,
        (now() at time zone 'utc')::timestamp AS last_updated_at,
        false AS is_live_data,
        CURRENT_DATE AS data_snapshot_date
    FROM routes r
    JOIN city_airports dep ON dep.city = r.from_city
    JOIN city_airports arr ON arr.city = r.to_city
    CROSS JOIN generate_series(0, 89) AS d(day_offset)
    CROSS JOIN flight_slots s
)
INSERT INTO provider_flights (
    id, flight_number, airline, airline_logo_url,
    departure_city, arrival_city, departure_airport_code, arrival_airport_code,
    departure_time, arrival_time, duration_minutes, price, currency_code,
    available_seats, provider_name, last_updated_at, is_live_data, data_snapshot_date
)
SELECT
    id, flight_number, airline, airline_logo_url,
    departure_city, arrival_city, departure_airport_code, arrival_airport_code,
    departure_time, arrival_time, duration_minutes, price, currency_code,
    available_seats, provider_name, last_updated_at, is_live_data, data_snapshot_date
FROM flight_rows
ON CONFLICT (id) DO UPDATE SET
    flight_number = EXCLUDED.flight_number,
    airline = EXCLUDED.airline,
    airline_logo_url = EXCLUDED.airline_logo_url,
    departure_city = EXCLUDED.departure_city,
    arrival_city = EXCLUDED.arrival_city,
    departure_airport_code = EXCLUDED.departure_airport_code,
    arrival_airport_code = EXCLUDED.arrival_airport_code,
    departure_time = EXCLUDED.departure_time,
    arrival_time = EXCLUDED.arrival_time,
    duration_minutes = EXCLUDED.duration_minutes,
    price = EXCLUDED.price,
    currency_code = EXCLUDED.currency_code,
    available_seats = EXCLUDED.available_seats,
    provider_name = EXCLUDED.provider_name,
    last_updated_at = EXCLUDED.last_updated_at,
    is_live_data = EXCLUDED.is_live_data,
    data_snapshot_date = EXCLUDED.data_snapshot_date;

WITH hotel_templates AS (
    SELECT *
    FROM (VALUES
        ('Paris', 'France', 'Left Bank Nest', 48.8499, 2.3470, 3, 8.4, 820, 88.00, 'https://example.com/omniflow/hotels/paris-left-bank'),
        ('Paris', 'France', 'Montmartre Rooms', 48.8867, 2.3431, 3, 8.1, 640, 96.00, 'https://example.com/omniflow/hotels/paris-montmartre'),
        ('Paris', 'France', 'Canal Saint-Martin Hotel', 48.8720, 2.3652, 4, 8.7, 1170, 138.00, 'https://example.com/omniflow/hotels/paris-canal'),
        ('Paris', 'France', 'Opera Grand Stay', 48.8719, 2.3316, 4, 9.0, 1490, 175.00, 'https://example.com/omniflow/hotels/paris-opera'),
        ('Paris', 'France', 'Seine View Palace', 48.8589, 2.2945, 5, 9.3, 2310, 310.00, 'https://example.com/omniflow/hotels/paris-seine'),
        ('Paris', 'France', 'Latin Quarter Loft', 48.8462, 2.3447, 2, 7.9, 430, 72.00, 'https://example.com/omniflow/hotels/paris-latin'),
        ('Paris', 'France', 'Bastille Boutique', 48.8532, 2.3690, 4, 8.8, 980, 152.00, 'https://example.com/omniflow/hotels/paris-bastille'),
        ('Paris', 'France', 'Trocadero Premium Suites', 48.8627, 2.2884, 5, 9.2, 1710, 285.00, 'https://example.com/omniflow/hotels/paris-trocadero'),
        ('Paris', 'France', 'Gare du Nord Budget', 48.8809, 2.3553, 2, 7.5, 520, 65.00, 'https://example.com/omniflow/hotels/paris-budget'),
        ('Paris', 'France', 'Marais Urban House', 48.8586, 2.3622, 4, 8.9, 1350, 166.00, 'https://example.com/omniflow/hotels/paris-marais'),

        ('Rome', 'Italy', 'Trastevere Garden Hotel', 41.8893, 12.4698, 3, 8.5, 760, 82.00, 'https://example.com/omniflow/hotels/rome-trastevere'),
        ('Rome', 'Italy', 'Colosseo Central Rooms', 41.8902, 12.4922, 3, 8.3, 910, 95.00, 'https://example.com/omniflow/hotels/rome-colosseo'),
        ('Rome', 'Italy', 'Pantheon Residence', 41.8986, 12.4769, 4, 8.9, 1410, 144.00, 'https://example.com/omniflow/hotels/rome-pantheon'),
        ('Rome', 'Italy', 'Vatican Terrace Hotel', 41.9029, 12.4534, 4, 8.8, 1200, 156.00, 'https://example.com/omniflow/hotels/rome-vatican'),
        ('Rome', 'Italy', 'Spanish Steps Palace', 41.9058, 12.4823, 5, 9.4, 2100, 290.00, 'https://example.com/omniflow/hotels/rome-spanish-steps'),
        ('Rome', 'Italy', 'Termini Smart Stay', 41.9010, 12.5010, 2, 7.6, 480, 58.00, 'https://example.com/omniflow/hotels/rome-termini'),
        ('Rome', 'Italy', 'Campo de Fiori Boutique', 41.8955, 12.4722, 4, 8.6, 870, 130.00, 'https://example.com/omniflow/hotels/rome-campo'),
        ('Rome', 'Italy', 'Aventino Quiet Suites', 41.8836, 12.4829, 5, 9.1, 990, 245.00, 'https://example.com/omniflow/hotels/rome-aventino'),
        ('Rome', 'Italy', 'Monti Budget Inn', 41.8958, 12.4917, 2, 7.8, 390, 62.00, 'https://example.com/omniflow/hotels/rome-monti'),
        ('Rome', 'Italy', 'Navona Art Hotel', 41.8992, 12.4731, 4, 8.7, 1180, 150.00, 'https://example.com/omniflow/hotels/rome-navona'),

        ('Istanbul', 'Turkey', 'Sultanahmet Heritage Hotel', 41.0055, 28.9768, 4, 8.9, 1320, 92.00, 'https://example.com/omniflow/hotels/istanbul-sultanahmet'),
        ('Istanbul', 'Turkey', 'Karakoy Port Rooms', 41.0256, 28.9744, 3, 8.4, 780, 68.00, 'https://example.com/omniflow/hotels/istanbul-karakoy'),
        ('Istanbul', 'Turkey', 'Galata View Suites', 41.0258, 28.9742, 4, 9.0, 1540, 120.00, 'https://example.com/omniflow/hotels/istanbul-galata'),
        ('Istanbul', 'Turkey', 'Bosphorus Premium Palace', 41.0430, 29.0340, 5, 9.4, 2400, 260.00, 'https://example.com/omniflow/hotels/istanbul-bosphorus'),
        ('Istanbul', 'Turkey', 'Kadikoy Market Hotel', 40.9903, 29.0291, 3, 8.2, 650, 55.00, 'https://example.com/omniflow/hotels/istanbul-kadikoy'),
        ('Istanbul', 'Turkey', 'Taksim Urban Stay', 41.0369, 28.9850, 4, 8.6, 1160, 86.00, 'https://example.com/omniflow/hotels/istanbul-taksim'),
        ('Istanbul', 'Turkey', 'Besiktas Boutique', 41.0438, 29.0079, 4, 8.7, 970, 105.00, 'https://example.com/omniflow/hotels/istanbul-besiktas'),
        ('Istanbul', 'Turkey', 'Old City Budget Inn', 41.0100, 28.9603, 2, 7.7, 410, 38.00, 'https://example.com/omniflow/hotels/istanbul-budget'),
        ('Istanbul', 'Turkey', 'Nisantasi Design Hotel', 41.0517, 28.9924, 5, 9.1, 890, 175.00, 'https://example.com/omniflow/hotels/istanbul-nisantasi'),
        ('Istanbul', 'Turkey', 'Moda Sea Rooms', 40.9838, 29.0254, 3, 8.5, 570, 61.00, 'https://example.com/omniflow/hotels/istanbul-moda'),

        ('Florence', 'Italy', 'Duomo Classic Hotel', 43.7731, 11.2560, 4, 8.8, 1090, 128.00, 'https://example.com/omniflow/hotels/florence-duomo'),
        ('Florence', 'Italy', 'Oltrarno Artist Rooms', 43.7653, 11.2470, 3, 8.4, 620, 84.00, 'https://example.com/omniflow/hotels/florence-oltrarno'),
        ('Florence', 'Italy', 'Santa Croce Boutique', 43.7687, 11.2626, 4, 8.9, 870, 136.00, 'https://example.com/omniflow/hotels/florence-santa-croce'),
        ('Florence', 'Italy', 'Arno Riverside Suites', 43.7679, 11.2531, 5, 9.2, 1430, 245.00, 'https://example.com/omniflow/hotels/florence-arno'),
        ('Florence', 'Italy', 'San Lorenzo Budget', 43.7762, 11.2535, 2, 7.8, 380, 58.00, 'https://example.com/omniflow/hotels/florence-budget'),
        ('Florence', 'Italy', 'Uffizi Grand Stay', 43.7687, 11.2559, 5, 9.3, 1970, 285.00, 'https://example.com/omniflow/hotels/florence-uffizi'),
        ('Florence', 'Italy', 'Boboli Garden Hotel', 43.7626, 11.2486, 4, 8.6, 760, 118.00, 'https://example.com/omniflow/hotels/florence-boboli'),
        ('Florence', 'Italy', 'Santa Maria Novella Inn', 43.7738, 11.2495, 3, 8.2, 520, 76.00, 'https://example.com/omniflow/hotels/florence-smn'),
        ('Florence', 'Italy', 'Ponte Vecchio View', 43.7680, 11.2531, 5, 9.1, 1330, 230.00, 'https://example.com/omniflow/hotels/florence-ponte'),
        ('Florence', 'Italy', 'Fiesole Hill Retreat', 43.8060, 11.2940, 4, 8.7, 660, 145.00, 'https://example.com/omniflow/hotels/florence-fiesole'),

        ('Amsterdam', 'Netherlands', 'Canal Ring Hotel', 52.3705, 4.8840, 4, 8.8, 1420, 148.00, 'https://example.com/omniflow/hotels/amsterdam-canal'),
        ('Amsterdam', 'Netherlands', 'Jordaan House', 52.3740, 4.8830, 3, 8.5, 890, 112.00, 'https://example.com/omniflow/hotels/amsterdam-jordaan'),
        ('Amsterdam', 'Netherlands', 'Museumplein Residence', 52.3584, 4.8811, 4, 8.9, 1240, 160.00, 'https://example.com/omniflow/hotels/amsterdam-museumplein'),
        ('Amsterdam', 'Netherlands', 'Dam Square Premium', 52.3731, 4.8925, 5, 9.2, 2040, 285.00, 'https://example.com/omniflow/hotels/amsterdam-dam'),
        ('Amsterdam', 'Netherlands', 'De Pijp Urban Rooms', 52.3548, 4.8944, 3, 8.3, 650, 96.00, 'https://example.com/omniflow/hotels/amsterdam-pijp'),
        ('Amsterdam', 'Netherlands', 'Centraal Budget Stay', 52.3791, 4.9003, 2, 7.6, 510, 74.00, 'https://example.com/omniflow/hotels/amsterdam-centraal'),
        ('Amsterdam', 'Netherlands', 'Vondelpark Suites', 52.3579, 4.8686, 5, 9.0, 1110, 230.00, 'https://example.com/omniflow/hotels/amsterdam-vondelpark'),
        ('Amsterdam', 'Netherlands', 'NDSM Creative Hotel', 52.4006, 4.8927, 4, 8.4, 720, 125.00, 'https://example.com/omniflow/hotels/amsterdam-ndsm'),
        ('Amsterdam', 'Netherlands', 'Nine Streets Boutique', 52.3699, 4.8849, 4, 8.7, 970, 152.00, 'https://example.com/omniflow/hotels/amsterdam-nine-streets'),
        ('Amsterdam', 'Netherlands', 'Waterlooplein Inn', 52.3676, 4.9020, 3, 8.1, 580, 88.00, 'https://example.com/omniflow/hotels/amsterdam-waterlooplein')
    ) AS h(city, country, hotel_name, latitude, longitude, stars, rating, review_count, base_price, provider_url)
),
hotel_rows AS (
    SELECT
        (
            substr(md5('hotel|' || h.city || '|' || h.hotel_name || '|' || d.day_offset), 1, 8) || '-' ||
            substr(md5('hotel|' || h.city || '|' || h.hotel_name || '|' || d.day_offset), 9, 4) || '-' ||
            substr(md5('hotel|' || h.city || '|' || h.hotel_name || '|' || d.day_offset), 13, 4) || '-' ||
            substr(md5('hotel|' || h.city || '|' || h.hotel_name || '|' || d.day_offset), 17, 4) || '-' ||
            substr(md5('hotel|' || h.city || '|' || h.hotel_name || '|' || d.day_offset), 21, 12)
        )::uuid AS id,
        h.hotel_name,
        h.city,
        h.country,
        h.latitude,
        h.longitude,
        h.stars,
        h.rating,
        h.review_count,
        CURRENT_DATE + d.day_offset AS valid_date,
        (h.base_price + ((d.day_offset % 10) * 3.00) + CASE WHEN EXTRACT(ISODOW FROM CURRENT_DATE + d.day_offset) IN (5,6) THEN 22 ELSE 0 END)::numeric(18,2) AS price_per_night,
        'EUR' AS currency_code,
        NULL::text AS thumbnail_url,
        'OmniMock' AS provider_name,
        h.provider_url,
        true AS is_available,
        (now() at time zone 'utc')::timestamp AS last_updated_at,
        false AS is_live_data,
        CURRENT_DATE AS data_snapshot_date
    FROM hotel_templates h
    CROSS JOIN generate_series(0, 89) AS d(day_offset)
)
INSERT INTO provider_hotels (
    id, hotel_name, city, country, latitude, longitude, stars, rating,
    review_count, valid_date, price_per_night, currency_code, thumbnail_url,
    provider_name, provider_url, is_available, last_updated_at, is_live_data, data_snapshot_date
)
SELECT
    id, hotel_name, city, country, latitude, longitude, stars, rating,
    review_count, valid_date, price_per_night, currency_code, thumbnail_url,
    provider_name, provider_url, is_available, last_updated_at, is_live_data, data_snapshot_date
FROM hotel_rows
ON CONFLICT (id) DO UPDATE SET
    hotel_name = EXCLUDED.hotel_name,
    city = EXCLUDED.city,
    country = EXCLUDED.country,
    latitude = EXCLUDED.latitude,
    longitude = EXCLUDED.longitude,
    stars = EXCLUDED.stars,
    rating = EXCLUDED.rating,
    review_count = EXCLUDED.review_count,
    valid_date = EXCLUDED.valid_date,
    price_per_night = EXCLUDED.price_per_night,
    currency_code = EXCLUDED.currency_code,
    thumbnail_url = EXCLUDED.thumbnail_url,
    provider_name = EXCLUDED.provider_name,
    provider_url = EXCLUDED.provider_url,
    is_available = EXCLUDED.is_available,
    last_updated_at = EXCLUDED.last_updated_at,
    is_live_data = EXCLUDED.is_live_data,
    data_snapshot_date = EXCLUDED.data_snapshot_date;

WITH place_seed AS (
    SELECT *
    FROM (VALUES
        ('Paris','France','Eiffel Tower','Tower',48.8584,2.2945,'Champ de Mars',35,false,ARRAY['Standard','Premium']::text[],ARRAY['Cultural','Influencer']::text[],120,4.8,240000),
        ('Paris','France','Louvre Museum','Museum',48.8606,2.3376,'Rue de Rivoli',22,false,ARRAY['Standard']::text[],ARRAY['Cultural','Cultural']::text[],180,4.7,310000),
        ('Paris','France','Notre-Dame Cathedral','Church',48.8530,2.3499,'Ile de la Cite',0,true,ARRAY['Economy']::text[],ARRAY['Cultural','Cultural']::text[],75,4.7,98000),
        ('Paris','France','Montmartre Walk','Viewpoint',48.8867,2.3431,'Montmartre',0,true,ARRAY['Economy']::text[],ARRAY['Local','Influencer']::text[],90,4.6,42000),
        ('Paris','France','Musee d Orsay','Museum',48.8600,2.3266,'Rue de la Legion d Honneur',18,false,ARRAY['Standard']::text[],ARRAY['Cultural','Cultural']::text[],150,4.8,95000),
        ('Paris','France','Luxembourg Gardens','Park',48.8462,2.3372,'Rue de Medicis',0,true,ARRAY['Economy']::text[],ARRAY['Nature','Relax']::text[],80,4.7,52000),
        ('Paris','France','Sainte-Chapelle','Church',48.8554,2.3450,'Boulevard du Palais',13,false,ARRAY['Economy','Standard']::text[],ARRAY['Cultural','Cultural']::text[],60,4.7,45000),
        ('Paris','France','Le Marais Food Street','Restaurant',48.8575,2.3580,'Le Marais',28,false,ARRAY['Standard']::text[],ARRAY['Gastronomy','Local']::text[],90,4.5,18500),
        ('Paris','France','Canal Saint-Martin','Bridge',48.8717,2.3653,'Canal Saint-Martin',0,true,ARRAY['Economy']::text[],ARRAY['Local','Influencer']::text[],70,4.4,21000),
        ('Paris','France','Palais Garnier','Theater',48.8719,2.3316,'Place de l Opera',17,false,ARRAY['Standard','Premium']::text[],ARRAY['Cultural','Cultural']::text[],90,4.7,72000),
        ('Paris','France','Tuileries Garden','Park',48.8635,2.3270,'Place de la Concorde',0,true,ARRAY['Economy']::text[],ARRAY['Nature','Relax']::text[],65,4.6,41000),
        ('Paris','France','Arc de Triomphe','Monument',48.8738,2.2950,'Place Charles de Gaulle',16,false,ARRAY['Standard']::text[],ARRAY['Cultural','Influencer']::text[],75,4.7,112000),
        ('Paris','France','Centre Pompidou','Gallery',48.8606,2.3522,'Place Georges-Pompidou',15,false,ARRAY['Standard']::text[],ARRAY['Cultural','Cultural']::text[],130,4.5,61000),
        ('Paris','France','Rue Cler Market','Market',48.8571,2.3062,'Rue Cler',12,false,ARRAY['Economy','Standard']::text[],ARRAY['Gastronomy','Local']::text[],70,4.5,16000),
        ('Paris','France','Shakespeare and Company','Shopping',48.8526,2.3471,'Rue de la Bucherie',10,false,ARRAY['Economy']::text[],ARRAY['Cultural','Shopping']::text[],45,4.6,28000),
        ('Paris','France','Bastille Night Bars','Bar',48.8530,2.3690,'Bastille',30,false,ARRAY['Standard']::text[],ARRAY['Nightlife','Local']::text[],120,4.3,12000),
        ('Paris','France','La Villette Park','Park',48.8938,2.3900,'Parc de la Villette',0,true,ARRAY['Economy']::text[],ARRAY['Nature','Local']::text[],120,4.4,24000),
        ('Paris','France','Pere Lachaise Cemetery','Memorial',48.8614,2.3933,'Boulevard de Menilmontant',0,true,ARRAY['Economy']::text[],ARRAY['Cultural','Cultural']::text[],100,4.6,36000),
        ('Paris','France','Galeries Lafayette','Mall',48.8738,2.3321,'Boulevard Haussmann',35,false,ARRAY['Standard','Premium']::text[],ARRAY['Shopping']::text[],90,4.4,70000),
        ('Paris','France','Seine Sunset Walk','Attraction',48.8580,2.3370,'Seine River',0,true,ARRAY['Economy']::text[],ARRAY['Romantic','Influencer']::text[],90,4.8,30000),

        ('Rome','Italy','Colosseum','Monument',41.8902,12.4922,'Piazza del Colosseo',18,false,ARRAY['Standard']::text[],ARRAY['Cultural','Cultural']::text[],150,4.7,380000),
        ('Rome','Italy','Roman Forum','Cultural',41.8925,12.4853,'Via della Salara Vecchia',18,false,ARRAY['Standard']::text[],ARRAY['Cultural','Cultural']::text[],150,4.7,160000),
        ('Rome','Italy','Pantheon','Church',41.8986,12.4769,'Piazza della Rotonda',5,false,ARRAY['Economy']::text[],ARRAY['Cultural','Cultural']::text[],60,4.8,210000),
        ('Rome','Italy','Trevi Fountain','Attraction',41.9009,12.4833,'Piazza di Trevi',0,true,ARRAY['Economy']::text[],ARRAY['Influencer','Romantic']::text[],45,4.8,270000),
        ('Rome','Italy','Vatican Museums','Museum',41.9065,12.4536,'Viale Vaticano',25,false,ARRAY['Standard','Premium']::text[],ARRAY['Cultural','Cultural']::text[],210,4.6,190000),
        ('Rome','Italy','Spanish Steps','Monument',41.9059,12.4823,'Piazza di Spagna',0,true,ARRAY['Economy']::text[],ARRAY['Influencer','Local']::text[],50,4.6,155000),
        ('Rome','Italy','Piazza Navona','Attraction',41.8992,12.4731,'Piazza Navona',0,true,ARRAY['Economy']::text[],ARRAY['Cultural','Local']::text[],70,4.7,135000),
        ('Rome','Italy','Trastevere Food Walk','Restaurant',41.8893,12.4698,'Trastevere',32,false,ARRAY['Standard']::text[],ARRAY['Gastronomy','Nightlife']::text[],120,4.6,27000),
        ('Rome','Italy','Villa Borghese','Park',41.9142,12.4922,'Villa Borghese',0,true,ARRAY['Economy']::text[],ARRAY['Nature','Relax']::text[],100,4.6,75000),
        ('Rome','Italy','Borghese Gallery','Gallery',41.9142,12.4922,'Piazzale Scipione Borghese',15,false,ARRAY['Standard']::text[],ARRAY['Cultural','Cultural']::text[],130,4.7,62000),
        ('Rome','Italy','Campo de Fiori Market','Market',41.8955,12.4722,'Campo de Fiori',12,false,ARRAY['Economy','Standard']::text[],ARRAY['Gastronomy','Local']::text[],70,4.4,42000),
        ('Rome','Italy','Castel Sant Angelo','Castle',41.9031,12.4663,'Lungotevere Castello',16,false,ARRAY['Standard']::text[],ARRAY['Cultural','Cultural']::text[],110,4.7,98000),
        ('Rome','Italy','Aventine Keyhole','Viewpoint',41.8833,12.4780,'Piazza dei Cavalieri di Malta',0,true,ARRAY['Economy']::text[],ARRAY['Influencer','Local']::text[],35,4.5,16000),
        ('Rome','Italy','Testaccio Market','Market',41.8777,12.4750,'Via Aldo Manuzio',18,false,ARRAY['Economy','Standard']::text[],ARRAY['Gastronomy','Local']::text[],85,4.6,24000),
        ('Rome','Italy','Capitoline Museums','Museum',41.8933,12.4829,'Piazza del Campidoglio',16,false,ARRAY['Standard']::text[],ARRAY['Cultural','Cultural']::text[],140,4.7,52000),
        ('Rome','Italy','Janiculum Terrace','Viewpoint',41.8919,12.4617,'Gianicolo',0,true,ARRAY['Economy']::text[],ARRAY['Influencer','Romantic']::text[],70,4.7,31000),
        ('Rome','Italy','Monti Wine Bars','Bar',41.8958,12.4917,'Rione Monti',28,false,ARRAY['Standard']::text[],ARRAY['Nightlife','Local']::text[],100,4.4,17000),
        ('Rome','Italy','Appian Way','Cultural',41.8586,12.5169,'Via Appia Antica',0,true,ARRAY['Economy']::text[],ARRAY['Adventure','Cultural']::text[],160,4.7,40000),
        ('Rome','Italy','MAXXI Museum','Museum',41.9285,12.4663,'Via Guido Reni',12,false,ARRAY['Standard']::text[],ARRAY['Cultural','Cultural']::text[],120,4.4,30000),
        ('Rome','Italy','Orange Garden','Park',41.8841,12.4803,'Giardino degli Aranci',0,true,ARRAY['Economy']::text[],ARRAY['Nature','Relax']::text[],60,4.7,28000),

        ('Istanbul','Turkey','Hagia Sophia','Cultural',41.0086,28.9802,'Sultanahmet',0,true,ARRAY['Economy']::text[],ARRAY['Cultural','Cultural']::text[],90,4.8,250000),
        ('Istanbul','Turkey','Blue Mosque','Church',41.0054,28.9768,'Sultanahmet',0,true,ARRAY['Economy']::text[],ARRAY['Cultural','Cultural']::text[],70,4.7,180000),
        ('Istanbul','Turkey','Topkapi Palace','Museum',41.0115,28.9833,'Cankurtaran',22,false,ARRAY['Standard']::text[],ARRAY['Cultural','Cultural']::text[],180,4.7,130000),
        ('Istanbul','Turkey','Grand Bazaar','Market',41.0107,28.9681,'Beyazit',20,false,ARRAY['Economy','Standard']::text[],ARRAY['Shopping','Local']::text[],120,4.4,170000),
        ('Istanbul','Turkey','Galata Tower','Tower',41.0256,28.9741,'Galata',18,false,ARRAY['Standard']::text[],ARRAY['Influencer','Cultural']::text[],80,4.6,150000),
        ('Istanbul','Turkey','Bosphorus Ferry','Attraction',41.0270,28.9769,'Eminonu Pier',8,false,ARRAY['Economy']::text[],ARRAY['Influencer','Local']::text[],120,4.8,92000),
        ('Istanbul','Turkey','Kadikoy Market','Market',40.9903,29.0291,'Kadikoy',16,false,ARRAY['Economy','Standard']::text[],ARRAY['Gastronomy','Local']::text[],100,4.6,41000),
        ('Istanbul','Turkey','Dolmabahce Palace','Museum',41.0392,29.0007,'Besiktas',20,false,ARRAY['Standard']::text[],ARRAY['Cultural','Cultural']::text[],140,4.7,98000),
        ('Istanbul','Turkey','Istiklal Street','Shopping',41.0340,28.9779,'Beyoglu',18,false,ARRAY['Economy','Standard']::text[],ARRAY['Shopping','Nightlife']::text[],110,4.4,120000),
        ('Istanbul','Turkey','Suleymaniye Mosque','Cultural',41.0162,28.9638,'Fatih',0,true,ARRAY['Economy']::text[],ARRAY['Cultural','Cultural']::text[],75,4.8,85000),
        ('Istanbul','Turkey','Balat Color Streets','Attraction',41.0290,28.9487,'Balat',0,true,ARRAY['Economy']::text[],ARRAY['Influencer','Local']::text[],80,4.5,22000),
        ('Istanbul','Turkey','Ortakoy Square','Attraction',41.0472,29.0269,'Ortakoy',12,false,ARRAY['Economy','Standard']::text[],ARRAY['Gastronomy','Influencer']::text[],80,4.6,55000),
        ('Istanbul','Turkey','Basilica Cistern','Cultural',41.0084,28.9779,'Sultanahmet',16,false,ARRAY['Standard']::text[],ARRAY['Cultural','Cultural']::text[],70,4.7,90000),
        ('Istanbul','Turkey','Moda Seaside Walk','Park',40.9789,29.0273,'Moda',0,true,ARRAY['Economy']::text[],ARRAY['Relax','Local']::text[],90,4.7,30000),
        ('Istanbul','Turkey','Cihangir Cafes','Cafe',41.0320,28.9822,'Cihangir',12,false,ARRAY['Economy','Standard']::text[],ARRAY['Gastronomy','Local']::text[],70,4.5,16000),
        ('Istanbul','Turkey','Emirgan Park','Park',41.1082,29.0548,'Emirgan',0,true,ARRAY['Economy']::text[],ARRAY['Nature','Relax']::text[],120,4.7,36000),
        ('Istanbul','Turkey','Pierre Loti Hill','Viewpoint',41.0471,28.9339,'Eyup',10,false,ARRAY['Economy']::text[],ARRAY['Influencer','Relax']::text[],90,4.5,43000),
        ('Istanbul','Turkey','Nisantasi Boutiques','Shopping',41.0517,28.9924,'Nisantasi',35,false,ARRAY['Standard','Premium']::text[],ARRAY['Shopping','Local']::text[],100,4.4,18000),
        ('Istanbul','Turkey','Karakoy Nightlife','Bar',41.0256,28.9744,'Karakoy',28,false,ARRAY['Standard']::text[],ARRAY['Nightlife','Local']::text[],110,4.4,21000),
        ('Istanbul','Turkey','Princes Islands Day Trip','Nature',40.8746,29.1287,'Buyukada',15,false,ARRAY['Standard']::text[],ARRAY['Nature','Relax']::text[],240,4.6,50000),

        ('Florence','Italy','Florence Cathedral','Church',43.7731,11.2560,'Piazza del Duomo',18,false,ARRAY['Standard']::text[],ARRAY['Cultural','Cultural']::text[],120,4.8,140000),
        ('Florence','Italy','Uffizi Gallery','Gallery',43.7687,11.2559,'Piazzale degli Uffizi',25,false,ARRAY['Standard','Premium']::text[],ARRAY['Cultural','Cultural']::text[],180,4.7,110000),
        ('Florence','Italy','Ponte Vecchio','Bridge',43.7680,11.2531,'Ponte Vecchio',0,true,ARRAY['Economy']::text[],ARRAY['Influencer','Romantic']::text[],50,4.7,125000),
        ('Florence','Italy','Piazzale Michelangelo','Viewpoint',43.7629,11.2652,'Piazzale Michelangelo',0,true,ARRAY['Economy']::text[],ARRAY['Influencer','Romantic']::text[],80,4.8,95000),
        ('Florence','Italy','Accademia Gallery','Gallery',43.7768,11.2586,'Via Ricasoli',16,false,ARRAY['Standard']::text[],ARRAY['Cultural','Cultural']::text[],100,4.6,82000),
        ('Florence','Italy','Boboli Gardens','Park',43.7626,11.2486,'Piazza de Pitti',10,false,ARRAY['Economy','Standard']::text[],ARRAY['Nature','Relax']::text[],120,4.5,43000),
        ('Florence','Italy','Mercato Centrale','Market',43.7767,11.2530,'San Lorenzo',18,false,ARRAY['Economy','Standard']::text[],ARRAY['Gastronomy','Local']::text[],90,4.5,62000),
        ('Florence','Italy','Palazzo Pitti','Museum',43.7652,11.2500,'Piazza de Pitti',16,false,ARRAY['Standard']::text[],ARRAY['Cultural','Cultural']::text[],130,4.6,70000),
        ('Florence','Italy','Santa Croce Basilica','Church',43.7687,11.2626,'Piazza Santa Croce',8,false,ARRAY['Economy']::text[],ARRAY['Cultural','Cultural']::text[],75,4.7,45000),
        ('Florence','Italy','Oltrarno Artisan Walk','Shopping',43.7653,11.2470,'Oltrarno',25,false,ARRAY['Standard']::text[],ARRAY['Shopping','Local']::text[],100,4.5,12000),
        ('Florence','Italy','San Miniato al Monte','Church',43.7621,11.2636,'Via delle Porte Sante',0,true,ARRAY['Economy']::text[],ARRAY['Cultural','Influencer']::text[],70,4.8,32000),
        ('Florence','Italy','Bargello Museum','Museum',43.7705,11.2577,'Via del Proconsolo',12,false,ARRAY['Standard']::text[],ARRAY['Cultural','Cultural']::text[],100,4.6,28000),
        ('Florence','Italy','Santo Spirito Square','Attraction',43.7660,11.2480,'Piazza Santo Spirito',0,true,ARRAY['Economy']::text[],ARRAY['Local','Nightlife']::text[],80,4.5,19000),
        ('Florence','Italy','Fiesole Hills','Mountain',43.8060,11.2940,'Fiesole',8,false,ARRAY['Economy','Standard']::text[],ARRAY['Nature','Adventure']::text[],150,4.7,21000),
        ('Florence','Italy','Gucci Garden','Museum',43.7696,11.2576,'Piazza della Signoria',12,false,ARRAY['Standard','Premium']::text[],ARRAY['Shopping','Cultural']::text[],80,4.3,18000),
        ('Florence','Italy','Arno River Sunset','Attraction',43.7679,11.2531,'Lungarno',0,true,ARRAY['Economy']::text[],ARRAY['Romantic','Influencer']::text[],60,4.7,14000),
        ('Florence','Italy','Santa Maria Novella','Church',43.7745,11.2493,'Piazza Santa Maria Novella',8,false,ARRAY['Economy']::text[],ARRAY['Cultural','Cultural']::text[],80,4.6,34000),
        ('Florence','Italy','La Specola Museum','Museum',43.7653,11.2489,'Via Romana',10,false,ARRAY['Economy','Standard']::text[],ARRAY['Cultural','Cultural']::text[],90,4.4,12000),
        ('Florence','Italy','Rooftop Aperitivo','Bar',43.7710,11.2550,'Historic Center',24,false,ARRAY['Standard']::text[],ARRAY['Nightlife','Romantic']::text[],90,4.5,10000),
        ('Florence','Italy','Leather Market','Market',43.7760,11.2534,'San Lorenzo',20,false,ARRAY['Economy','Standard']::text[],ARRAY['Shopping','Local']::text[],75,4.3,29000),

        ('Amsterdam','Netherlands','Rijksmuseum','Museum',52.3600,4.8852,'Museumstraat',22,false,ARRAY['Standard']::text[],ARRAY['Cultural','Cultural']::text[],150,4.7,150000),
        ('Amsterdam','Netherlands','Van Gogh Museum','Museum',52.3584,4.8811,'Museumplein',22,false,ARRAY['Standard']::text[],ARRAY['Cultural','Cultural']::text[],130,4.6,130000),
        ('Amsterdam','Netherlands','Anne Frank House','Museum',52.3752,4.8840,'Prinsengracht',16,false,ARRAY['Standard']::text[],ARRAY['Cultural','Cultural']::text[],90,4.5,95000),
        ('Amsterdam','Netherlands','Canal Ring Walk','Bridge',52.3702,4.8952,'Canal Ring',0,true,ARRAY['Economy']::text[],ARRAY['Influencer','Romantic']::text[],120,4.8,70000),
        ('Amsterdam','Netherlands','Vondelpark','Park',52.3579,4.8686,'Vondelpark',0,true,ARRAY['Economy']::text[],ARRAY['Nature','Relax']::text[],100,4.7,82000),
        ('Amsterdam','Netherlands','Jordaan Cafes','Cafe',52.3740,4.8830,'Jordaan',14,false,ARRAY['Economy','Standard']::text[],ARRAY['Gastronomy','Local']::text[],80,4.6,26000),
        ('Amsterdam','Netherlands','Dam Square','Attraction',52.3731,4.8925,'Dam',0,true,ARRAY['Economy']::text[],ARRAY['Cultural','Influencer']::text[],50,4.4,90000),
        ('Amsterdam','Netherlands','A DAM Lookout','Viewpoint',52.3840,4.9020,'Overhoeksplein',16,false,ARRAY['Standard']::text[],ARRAY['Influencer','Adventure']::text[],80,4.5,41000),
        ('Amsterdam','Netherlands','Albert Cuyp Market','Market',52.3558,4.8955,'Albert Cuypstraat',16,false,ARRAY['Economy','Standard']::text[],ARRAY['Gastronomy','Local']::text[],90,4.5,58000),
        ('Amsterdam','Netherlands','NDSM Wharf','Gallery',52.4006,4.8927,'NDSM',0,true,ARRAY['Economy']::text[],ARRAY['Cultural','Influencer']::text[],100,4.5,23000),
        ('Amsterdam','Netherlands','Heineken Experience','Attraction',52.3579,4.8919,'Stadhouderskade',21,false,ARRAY['Standard']::text[],ARRAY['Nightlife','Gastronomy']::text[],110,4.3,68000),
        ('Amsterdam','Netherlands','Begijnhof','Cultural',52.3690,4.8897,'Begijnhof',0,true,ARRAY['Economy']::text[],ARRAY['Cultural','Cultural']::text[],45,4.5,38000),
        ('Amsterdam','Netherlands','Nine Streets Shops','Shopping',52.3699,4.8849,'De 9 Straatjes',30,false,ARRAY['Standard','Premium']::text[],ARRAY['Shopping','Local']::text[],100,4.5,31000),
        ('Amsterdam','Netherlands','Foodhallen','Restaurant',52.3677,4.8686,'Bellamyplein',24,false,ARRAY['Standard']::text[],ARRAY['Gastronomy','Local']::text[],90,4.5,45000),
        ('Amsterdam','Netherlands','Rembrandt House','Museum',52.3693,4.9010,'Jodenbreestraat',17,false,ARRAY['Standard']::text[],ARRAY['Cultural','Cultural']::text[],90,4.5,35000),
        ('Amsterdam','Netherlands','Westerpark','Park',52.3868,4.8752,'Westerpark',0,true,ARRAY['Economy']::text[],ARRAY['Nature','Relax']::text[],90,4.6,28000),
        ('Amsterdam','Netherlands','Leidseplein Nightlife','Bar',52.3640,4.8830,'Leidseplein',30,false,ARRAY['Standard']::text[],ARRAY['Nightlife']::text[],120,4.3,39000),
        ('Amsterdam','Netherlands','ARTIS Zoo','Zoo',52.3663,4.9165,'Plantage Kerklaan',25,false,ARRAY['Standard']::text[],ARRAY['Local','Nature']::text[],180,4.5,72000),
        ('Amsterdam','Netherlands','Amsterdam Noord Ferry','Transport',52.3791,4.9003,'Centraal Station',0,true,ARRAY['Economy']::text[],ARRAY['Local','Influencer']::text[],45,4.6,22000),
        ('Amsterdam','Netherlands','Bloemenmarkt','Market',52.3667,4.8937,'Singel',18,false,ARRAY['Economy','Standard']::text[],ARRAY['Shopping','Local']::text[],60,4.2,54000)
    ) AS p(city, country, name, category, latitude, longitude, address, estimated_price, is_free, budget_tiers, travel_styles, duration_minutes, rating, review_count)
),
place_rows AS (
    SELECT
        (
            substr(md5('place|' || city || '|' || name), 1, 8) || '-' ||
            substr(md5('place|' || city || '|' || name), 9, 4) || '-' ||
            substr(md5('place|' || city || '|' || name), 13, 4) || '-' ||
            substr(md5('place|' || city || '|' || name), 17, 4) || '-' ||
            substr(md5('place|' || city || '|' || name), 21, 12)
        )::uuid AS id,
        *
    FROM place_seed
)
INSERT INTO places (
    id, name, description, category, photo_url, phone, website_url,
    latitude, longitude, address, city, country, timezone, google_place_id,
    estimated_price, currency_code, is_free, budget_tiers, travel_styles,
    duration_minutes, rating, opening_hours, best_months, is_active,
    cuisine, fee, heritage, image, wheelchair, wikidata, wikipedia,
    photo_urls, price_level, review_count, google_tags
)
SELECT
    id,
    name,
    'OmniMock curated demo place for ' || city AS description,
    category,
    NULL::text AS photo_url,
    NULL::text AS phone,
    NULL::text AS website_url,
    latitude,
    longitude,
    address,
    city,
    country,
    NULL::text AS timezone,
    'omniflow-mock-' || replace(lower(city), ' ', '-') || '-' || replace(lower(name), ' ', '-') AS google_place_id,
    estimated_price::numeric(18,2),
    'EUR' AS currency_code,
    is_free,
    budget_tiers,
    travel_styles,
    duration_minutes,
    rating::numeric(3,1),
    NULL::jsonb AS opening_hours,
    ARRAY[1,2,3,4,5,6,7,8,9,10,11,12]::integer[] AS best_months,
    true AS is_active,
    NULL::text AS cuisine,
    CASE WHEN is_free THEN 'no' ELSE 'yes' END AS fee,
    NULL::text AS heritage,
    NULL::text AS image,
    NULL::text AS wheelchair,
    NULL::text AS wikidata,
    NULL::text AS wikipedia,
    ARRAY[]::text[] AS photo_urls,
    CASE
        WHEN estimated_price = 0 THEN 0
        WHEN estimated_price < 15 THEN 1
        WHEN estimated_price < 30 THEN 2
        ELSE 3
    END AS price_level,
    review_count,
    travel_styles AS google_tags
FROM place_rows
ON CONFLICT (id) DO UPDATE SET
    name = EXCLUDED.name,
    description = EXCLUDED.description,
    category = EXCLUDED.category,
    latitude = EXCLUDED.latitude,
    longitude = EXCLUDED.longitude,
    address = EXCLUDED.address,
    city = EXCLUDED.city,
    country = EXCLUDED.country,
    google_place_id = EXCLUDED.google_place_id,
    estimated_price = EXCLUDED.estimated_price,
    currency_code = EXCLUDED.currency_code,
    is_free = EXCLUDED.is_free,
    budget_tiers = EXCLUDED.budget_tiers,
    travel_styles = EXCLUDED.travel_styles,
    duration_minutes = EXCLUDED.duration_minutes,
    rating = EXCLUDED.rating,
    best_months = EXCLUDED.best_months,
    is_active = EXCLUDED.is_active,
    fee = EXCLUDED.fee,
    photo_urls = EXCLUDED.photo_urls,
    price_level = EXCLUDED.price_level,
    review_count = EXCLUDED.review_count,
    google_tags = EXCLUDED.google_tags;

COMMIT;

-- Quick sanity checks after running:
-- SELECT provider_name, count(*) FROM provider_flights GROUP BY provider_name;
-- SELECT provider_name, count(*) FROM provider_hotels GROUP BY provider_name;
-- SELECT city, count(*) FROM places WHERE google_place_id LIKE 'omniflow-mock-%' GROUP BY city ORDER BY city;
