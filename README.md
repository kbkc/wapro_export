# wapro_export
en-----------------
app config via app_cfg.xml

app started with 3 operation modes:

--------------------
mode 1. (<start_arg01> = 1 in  app_cfg.xml, no args)

silent mode. For starts via task sheduler

result:
from DB 'wapro' in MS SQL get new documents and writes them to the remote intermediate database in MySQL

new documents detects by comparing saved id in <s>id record</s> and in database 'wapro'
--------------------
mode 2. (<start_arg01> = 2 in app_cfg.xml, no args)
silent mode. For starts via task sheduler

result: gets new files from remote intermediate database in MySQL 
and writes them to the local folder (csv_out_dir in app_cfg.xml)

--------------------------------------------------------------------
mode 3. (command line arg needed)

used for encrytping of password (if used encryptor)
 in app_cfg.xml:
is_pass_encrypted = true - use enctypted passwords
is_pass_encrypted = false - not encripted

run: wapro_export.exe <password for encrypt>
result: encrypted_arg.txt contains encrypted and decrypted password

ru-----------------
Программа настраивается через файл app_cfg.xml
(описания некоторых настроек имеются здесь и в файле app_cfg.xml)



Программа работает в 3х режимах

--------
Режим 1. (значение <start_arg01> = 1 )
Запуск через планировщик заданий с нужной периодичностью

Результат запуска:
Из базы wapro забирает новые документы и пишет в промежуточную базу

Детали:
синхронизация происходит по одному из параметров <s>id записи</s> (параметр изменяется после каждого импорта докуметов)

--------
Режим 2. (значение <start_arg01> = 2 )
Запуск через планировщик заданий с нужной периодичностью

Результат запуска:
Из промежуточной базы забирает документы и пишет на диск в папку, прописанную в csv_out_dir

Детали:
в нашем случае вид такой: <csv_out_dir>D:\Bases_1C\Reforma\wapro_imp\</csv_out_dir>

--------
Режим 3. (требуется параметр командной строки)
Нужен для шифрования пароля, если в файле app_cfg.xml используется шифрование паролей 
(is_pass_encrypted = true - используются зашифрованные пароли, false - незашифрованные)

Запуск из командной строки с параметром(пароль, коорый нужно зашифровать).

Результат запуска:
создается файл encrypted_arg.txt, где записан пароль в обычном и зашифрованном виде.

