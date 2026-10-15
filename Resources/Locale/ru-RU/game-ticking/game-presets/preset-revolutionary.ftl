# SPDX-FileCopyrightText: 2023 DrSmugleaf <DrSmugleaf@users.noreply.github.com>
# SPDX-FileCopyrightText: 2023 Vasilis <vasilis@pikachu.systems>
# SPDX-FileCopyrightText: 2023 coolmankid12345 <55817627+coolmankid12345@users.noreply.github.com>
# SPDX-FileCopyrightText: 2023 coolmankid12345 <coolmankid12345@users.noreply.github.com>
# SPDX-FileCopyrightText: 2023 deltanedas <@deltanedas:kde.org>
# SPDX-FileCopyrightText: 2024 BombasterDS <115770678+BombasterDS@users.noreply.github.com>
# SPDX-FileCopyrightText: 2024 Killerqu00 <47712032+Killerqu00@users.noreply.github.com>
# SPDX-FileCopyrightText: 2024 Mr. 27 <45323883+Dutch-VanDerLinde@users.noreply.github.com>
# SPDX-FileCopyrightText: 2024 deltanedas <39013340+deltanedas@users.noreply.github.com>
# SPDX-FileCopyrightText: 2025 Aiden <28298836+Aidenkrz@users.noreply.github.com>
# SPDX-FileCopyrightText: 2025 Aiden <aiden@djkraz.com>
#
# SPDX-License-Identifier: AGPL-3.0-or-later

## Rev Head

roles-antag-rev-head-name = Глава революции
roles-antag-rev-head-objective = Ваша задача - захватить станцию, склонив членов экипажа на свою сторону, и уничтожив весь командный состав станции.

## Nebula
head-rev-role-greeting =
    Вы — Глава Революции.
    Ваша задача — убрать всё командование со станции: убийством, изгнанием или заключением.
    У вас уже есть компоненты для промышленной кузницы и производства нужных инструментов.
    Печатайте пропагандистские листовки на печатном станке, чтобы обращать экипаж.
    Будьте осторожны: ваши машины очень шумные. Вам придётся прятать и защищать базу операций.
    Viva la revolución!

## Trauma - rewrote
head-rev-briefing =
    Стройте революционные промышленные здания.
    Избавьтесь от всех глав, чтобы захватить станцию.

head-rev-break-mindshield = Щит разума был уничтожен!

## Rev

roles-antag-rev-name = Революционер
roles-antag-rev-objective = Ваша задача - защищать и выполнять приказы глав революции, а также избавиться от всего командного состава станции или конвертировать его.

rev-break-control =
    { $name } { GENDER($name) ->
        [male] вспомнил, кому он верен
        [female] вспомнила, кому она верна
        [epicene] вспомнили, кому они верни
       *[neuter] вспомнило, кому оно верно
    } на самом деле!

## Nebula
rev-role-greeting =
    Вы — революционер. Ваша задача — защищать глав революции и помогать им захватить станцию.
    Революция должна вместе убивать, удерживать или обращать всё командование.
    Viva la revolución!

rev-briefing = Помогите главам революции убить, удержать или обратить всё командование, чтобы захватить станцию.

## General

rev-title = Революционеры
rev-description = Революционеры среди нас.

rev-not-enough-ready-players = Недостаточно игроков готовы к игре! { $readyPlayersCount } игроков из необходимых { $minimumPlayers } готовы. Нельзя запустить пресет Революционеры.
rev-no-one-ready = Нет готовых игроков! Нельзя запустить пресет Революционеры.
rev-no-heads = Нет кандидатов на роль главы революции. Нельзя запустить пресет Революционеры.

rev-won = Главы революции выжили и уничтожили весь командный состав станции.

rev-lost = Члены командного состава станции выжили и уничтожили всех глав революции.

rev-stalemate = Главы революции и командный состав станции погибли. Это ничья.

rev-reverse-stalemate = Главы революции и командный состав станции выжили.

rev-headrev-count =
    { $initialCount ->
        [one] Глава революции был один:
       *[other] Глав революции было { $initialCount }:
    }

rev-headrev-name-user = [color=#5e9cff]{ $name }[/color] ([color=gray]{ $username }[/color]) конвертировал { $count } { $count ->
        [one] члена
        [few] члена
       *[other] членов
    } экипажа

rev-headrev-name = [color=#5e9cff]{ $name }[/color] конвертировал { $count } { $count ->
        [one] члена
        [few] члена
       *[other] членов
    } экипажа

## Deconverted window

rev-deconverted-title = Разконвертированы!
rev-deconverted-text =
    Со смертью последнего главы революции, революция оканчивается.
    
    Вы больше не революционер, так что ведите себя хорошо.
rev-deconverted-confirm = Подтвердить
