-- Kitap resimlerini güncelle
-- Bu script mevcut kitapların resimlerini günceller

-- C# Programlama kitabı
UPDATE Book 
SET imgfile = 'https://cdn1.dokuzsoft.com/u/pelikankitabevi/img/b/u/m/umuttepe-objective-c-programlama-dili-mimar-aslan338fc5c49effd2447e1a7eca9326c04a.jpg'
WHERE title = 'C# Programlama';

-- Veri Yapıları ve Algoritmalar kitabı
UPDATE Book 
SET imgfile = 'https://avatars.mds.yandex.net/i?id=37a40c159f9460310e6c39675a51cf3d7a9cf7c6-5876454-images-thumbs&n=13'
WHERE title = 'Veri Yapıları ve Algoritmalar';

-- Web Geliştirme kitabı
UPDATE Book 
SET imgfile = 'https://cdn.kitapsec.com/image/urun/2017/12/06/1512552355.jpg'
WHERE title = 'Web Geliştirme';

-- Eğer başka kitaplar varsa onlara da resim ekle
UPDATE Book 
SET imgfile = 'https://images.unsplash.com/photo-1481627834876-b7833e8f5570?w=400&h=600&fit=crop'
WHERE imgfile IS NULL OR imgfile = '';

-- Güncellenen kitapları kontrol et
SELECT Id, title, author, imgfile FROM Book; 