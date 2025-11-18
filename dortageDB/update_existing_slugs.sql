-- Mevcut projeler için slug oluşturma scripti
-- Bu scripti SQL Server Management Studio veya başka bir SQL araçla çalıştırın

UPDATE Projeler
SET Slug =
    LOWER(
        REPLACE(
            REPLACE(
                REPLACE(
                    REPLACE(
                        REPLACE(
                            REPLACE(
                                REPLACE(
                                    REPLACE(
                                        REPLACE(
                                            REPLACE(
                                                REPLACE(
                                                    REPLACE(
                                                        REPLACE(
                                                            ProjeAdi COLLATE Turkish_CI_AI,
                                                            'İ', 'i'
                                                        ),
                                                        'Ğ', 'g'
                                                    ),
                                                    'Ü', 'u'
                                                ),
                                                'Ş', 's'
                                            ),
                                            'Ö', 'o'
                                        ),
                                        'Ç', 'c'
                                    ),
                                    'I', 'i'
                                ),
                                ' ', '-'
                            ),
                            '.', ''
                        ),
                        ',', ''
                    ),
                    '/', '-'
                ),
                '\', '-'
            ),
            '(', ''
        ),
        ')', ''
    )
WHERE Slug IS NULL OR Slug = '';

-- Birden fazla tire olan yerleri tek tireye çevir
UPDATE Projeler
SET Slug = REPLACE(REPLACE(REPLACE(REPLACE(Slug, '-----', '-'), '----', '-'), '---', '-'), '--', '-')
WHERE Slug LIKE '%---%';

-- Başında ve sonunda tire olanları temizle
UPDATE Projeler
SET Slug = TRIM('-' FROM Slug)
WHERE Slug LIKE '-%' OR Slug LIKE '%-';

-- Sonuç kontrolü
SELECT ProjeID, ProjeAdi, Slug FROM Projeler ORDER BY ProjeID;
