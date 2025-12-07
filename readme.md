# tsweb-cli
Это консольный интерфейс для tsweb.

## usage
```
-l --login //логин
-sc --set-contest <constest_id> //установить контест
-lc --list-contests //список контестов
-st --set-task <task_index> //установить задачу
-lt --list-tasks //список задач
-sl --set-compiler <compiler_index> //установить компилятор
-ll --list-compilers //список компиляторов
-s --submit <file_path> //отправить посылку
-ls --list-submits //список посылок
-d --debug //режим дебага(выводится нативный HTML)
```
## build
`man dotnet`

## to-do
- шифрование cookie в config.json
- ~~сокрытие пароля при вводе~~
- улучшение обработки ввода пользователя
- мультиплатформа для config.json
- получение условий в pdf
- разобраться с AOT/self-contained/trimmed сборкой

## contribution
pull requests and issues are welcome