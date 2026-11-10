cmd-align-desc =
    Автоматически поворачивает все закреплённые шлюзы, двери, пожарные шлюзы и т.п.
    вдоль соседних конструкций.

    Параметр [dry run] выполняет только проверку, ничего не поворачивая.
cmd-align-help = Использование: {$command} [MapID] [dry run?]
cmd-align-no-release = Эту команду нельзя использовать в RELEASE-сборке.
cmd-align-hint-id = MapID
cmd-align-hint-dry = только проверка?
cmd-align-feedback-none = {$dry ->
[true] ПРОВЕРКА: не найдено
*[false] Не найдено
} сущностей, совместимых с AlignerSystem!
cmd-align-feedback-good = {$dry ->
[true] ПРОВЕРКА: не найдено
*[false] Не найдено
} неправильно повёрнутых сущностей.
cmd-align-feedback = {$dry ->
[true] ПРОВЕРКА: найдено
*[false] Найдено и исправлено
} {$fixed ->
[one] {$fixed} неправильно повёрнутая сущность.
[few] {$fixed} неправильно повёрнутые сущности.
*[other] {$fixed} неправильно повёрнутых сущностей.
}
