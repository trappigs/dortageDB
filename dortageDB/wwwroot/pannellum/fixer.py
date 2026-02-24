from PIL import Image

old_img = Image.open("kepsut.jpeg")
width, height = old_img.size

# Yeni 2:1 boyutlarını hesapla (Genişlik sabit kalsın dersek yükseklik yarıya inmeli, 
# ama biz genişliği 2 katına çıkarıp yüksekliği sabit tutalım ki detay kaybolmasın)
new_width = height * 2
new_height = height

# Siyah bir canvas oluştur
new_img = Image.new("RGB", (new_width, new_height), (0, 0, 0))

# Kare resmi tam ortaya yapıştır
offset = (int((new_width - width) / 2), 0)
new_img.paste(old_img, offset)

new_img.save("kepsut_fixed.jpg")
print("Görsel 2:1 oranına getirildi: kepsut_fixed.jpg")