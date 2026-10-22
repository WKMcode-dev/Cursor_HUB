"""Generate the BMFont binary atlas used by osu!framework from Kode Mono (OFL)."""
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont
import struct, sys
folder=Path(sys.argv[1]); folder.mkdir(parents=True,exist_ok=True)
font=ImageFont.truetype(str(folder/'KodeMono.ttf'),48)
image=Image.new('RGBA',(1024,2048)); draw=ImageDraw.Draw(image)
chars=bytearray()
for index,code in enumerate([*range(32,384),0x2026]):
    text=chr(code); left,top,right,bottom=font.getbbox(text)
    w,h=max(1,right-left),max(1,bottom-top)
    x,y=(index%16)*64+4,(index//16)*80+4
    draw.text((x-left,y-top),text,font=font,fill='white')
    chars+=struct.pack('<IHHHHhhhBB',code,x,y,w,h,left,top,round(font.getlength(text)),0,15)
def block(kind,data):return bytes([kind])+struct.pack('<I',len(data))+data
info=struct.pack('<hBBHBBBBBBBB',48,0,0,100,1,0,0,0,0,0,0,0)+b'KodeMono\0'
common=struct.pack('<HHHHHBBBBB',60,48,1024,2048,1,0,0,0,0,0)
(folder/'KodeMono.fnt').write_bytes(b'BMF\x03'+block(1,info)+block(2,common)+block(3,b'KodeMono_0.png\0')+block(4,chars))
image.save(folder/'KodeMono_0.png')
print('Kode Mono: 353 glyphs, 1024x2048 atlas')
