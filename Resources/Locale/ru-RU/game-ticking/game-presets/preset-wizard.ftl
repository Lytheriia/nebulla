## Survivor

roles-antag-survivor-name = Выживший
# It's a Halo reference
roles-antag-survivor-objective = Текущая задача: Выжить

survivor-role-greeting =
    Вы - выживший.
    Ваша главная задача остаться в живых и вернуться на ЦК.
    Накопите столько огневой мощи, сколько необходимо для гарантии вашего выживания.
    Никому не доверяйте.

survivor-round-end-dead-count =
    { $deadCount ->
        [one] [color=red]{ $deadCount }[/color] выживший умер.
       *[other] [color=red]{ $deadCount }[/color] выживших умерло.
    }

survivor-round-end-alive-count =
    { $aliveCount ->
        [one] [color=yellow]{ $aliveCount }[/color] выживший остался на станции.
       *[other] [color=yellow]{ $aliveCount }[/color] выживших осталось на станции.
    }

survivor-round-end-alive-on-shuttle-count =
    { $aliveCount ->
        [one] [color=green]{ $aliveCount }[/color] выживший выбрался живым.
       *[other] [color=green]{ $aliveCount }[/color] выживших выбралось живыми.
    }

## Wizard

objective-issuer-swf = [color=turquoise]Федерация космических волшебников[/color]

wizard-title = Волшебник
wizard-description = На станции волшебник! Никогда не знаешь, что он может выкинуть.

roles-antag-wizard-name = Волшебник
roles-antag-wizard-objective = Преподайте им урок, который они никогда не забудут.

## Nebula
wizard-role-greeting =
    Пора колдовать, файербол!
    Между Федерацией космических волшебников и NanoTrasen накалилась обстановка. Федерация выбрала вас, чтобы нанести визит на станцию и «напомнить», с чародеями шутки плохи.
    Чините хаос и разрушение! Что вы сделаете — решать вам, но помните: волшебники хотят, чтобы вы вернулись живым.

wizard-round-end-name = волшебник

## TODO: Wizard Apprentice (Coming sometime post-wizard release)
