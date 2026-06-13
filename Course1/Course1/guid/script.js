(function () {
    const translations = {
        ru: {
            pageTitle: 'Руководство по игре Drone Algorithm',
            eyebrow: 'Учебная игра по параллельному программированию',
            heroTitle: 'Руководство пользователя',
            heroLead: 'В игре нужно написать алгоритм для группы дронов так, чтобы они уничтожили все сорняки на карте. Команды выполняются по тикам: внутри одного тика несколько дронов могут действовать параллельно.',
            navCommands: 'Команды',
            navMaps: 'Карты',
            navAlgorithms: 'Алгоритмы',
            goalTitle: 'Цель игры',
            goalText: 'На карте находятся дроны и сорняки. Пользователь заполняет таблицу команд, запускает алгоритм и проверяет, сможет ли алгоритм уничтожить все сорняки без ошибок.',
            successTitle: 'Успешное завершение',
            successText: 'Все сорняки уничтожены, столкновений нет, дроны не вышли за границы карты.',
            errorTitle: 'Ошибка',
            errorText: 'Дрон врезался в границу, столкнулся с другим дроном, атаковал пустую клетку или алгоритм не уничтожил все сорняки.',
            commandsTitle: 'Как писать команды',
            commandsText: 'Таблица команд состоит из тиков. Один тик — это один шаг алгоритма. Команды, записанные в одном тике, выполняются параллельно.',
            tableTarget: 'Адресат',
            tableAction: 'Действие',
            tableArgument: 'Аргумент',
            tableDescription: 'Описание',
            targetAll: 'Все',
            targetDrone1: 'Дрон 1',
            targetDrone2: 'Дрон 2',
            targetDrone3: 'Дрон 3',
            actionForward: 'Вперёд',
            actionLeft: 'Налево',
            actionRight: 'Направо',
            actionAttack: 'Разряд',
            emptyOrOne: 'пусто или 1',
            descForward: 'Все дроны делают указанное количество шагов вперёд.',
            descLeft: 'Дрон поворачивается на 90° влево.',
            descRight: 'Дрон поворачивается на 90° вправо.',
            descAttack: 'Дрон уничтожает сорняк в клетке перед собой.',
            step1Title: 'Выберите адресата',
            step1Text: 'Можно выбрать конкретного дрона или команду «Все».',
            step2Title: 'Выберите действие',
            step2Text: 'Доступны движения, повороты и атака.',
            step3Title: 'Запустите алгоритм',
            step3Text: 'Нажмите «Выполнить» для полного запуска или «Выполнить пошагово», чтобы выполнять алгоритм по одному элементарному действию.',
            parallelTitle: 'Параллельное выполнение',
            parallelText: 'Если в одном тике заданы команды для нескольких дронов, они начинают выполняться одновременно. Для карт с большим количеством дронов тик может занимать несколько строк таблицы.',
            parallelCode: 'Тик 1: Дрон 1 — Вперёд    Дрон 2 — Вперёд\nТик 1: Дрон 3 — Разряд    Дрон 4 — Налево\nТик 2: Все    — Вперёд',
            parallelExample: 'В примере первые четыре команды относятся к одному тику, а команда «Все — Вперёд» выполнится уже следующим шагом.',
            scoreTitle: 'Оценка алгоритма',
            scoreText: 'После успешного завершения игра показывает оценку алгоритма.',
            formula: 'Оценка = повороты налево/направо + команды «Вперёд» × 2',
            scoreHint: 'Команда «Разряд» не увеличивает оценку. Чем ниже оценка, тем короче и эффективнее алгоритм.',
            mapsTitle: 'Как создавать карты',
            mapsText: 'Откройте меню <strong>Файл → Открыть редактор карт</strong>. В редакторе можно выбирать клетки и добавлять или удалять дронов и сорняки.',
            mapRule1: 'На карте должен быть хотя бы один дрон.',
            mapRule2: 'Дронов не может быть больше 10.',
            mapRule3: 'Количество сорняков не должно быть меньше количества дронов.',
            mapRule4: 'Дрон и сорняк нельзя поставить в одну и ту же клетку.',
            mapRule5: 'Карта сохраняется в папку <code>levels</code> в формате JSON.',
            showMapJson: 'Показать пример JSON карты',
            hideMapJson: 'Скрыть пример JSON карты',
            algorithmsTitle: 'Как сохранять и загружать алгоритмы',
            algorithmsText: 'Команды из таблицы можно сохранить через <strong>Файл → Сохранить алгоритм</strong>. Алгоритмы хранятся в папке <code>algorithms</code> в формате JSON.',
            algorithmRule1: 'При сохранении укажите название алгоритма.',
            algorithmRule2: 'При загрузке выберите JSON-файл из папки <code>algorithms</code>.',
            algorithmRule3: 'Алгоритм содержит количество дронов, для которого он был создан.',
            algorithmRule4: 'Если количество дронов на текущей карте другое, алгоритм не загрузится.',
            showAlgorithmJson: 'Показать пример JSON алгоритма',
            hideAlgorithmJson: 'Скрыть пример JSON алгоритма',
            tipsTitle: 'Советы',
            tip1: '<strong>Планируйте направление.</strong> Команда «Разряд» действует на клетку перед дроном, поэтому повороты важны.',
            tip2: '<strong>Проверяйте тики.</strong> Если несколько дронов должны действовать одновременно, помещайте команды в один тик.',
            tip3: '<strong>Следите за зарядами.</strong> Сумма зарядов дронов равна количеству сорняков, поэтому лишние атаки приведут к ошибке.',
            tip4: '<strong>Используйте тестовые алгоритмы.</strong> Они помогают проверить успешное прохождение, столкновение, выход за карту и неполное уничтожение сорняков.',
            toTopLabel: 'Наверх',
            languageButton: 'EN'
        },
        en: {
            pageTitle: 'Drone Algorithm Game Guide',
            eyebrow: 'Educational game about parallel programming',
            heroTitle: 'User Guide',
            heroLead: 'In this game, you write an algorithm for a group of drones so they can destroy all weeds on the map. Commands are executed by ticks: within one tick, several drones may act in parallel.',
            navCommands: 'Commands',
            navMaps: 'Maps',
            navAlgorithms: 'Algorithms',
            goalTitle: 'Game objective',
            goalText: 'The map contains drones and weeds. The user fills in the command table, runs the algorithm, and checks whether the algorithm can destroy all weeds without errors.',
            successTitle: 'Successful completion',
            successText: 'All weeds are destroyed, there are no collisions, and no drone leaves the map boundaries.',
            errorTitle: 'Error',
            errorText: 'A drone hits the boundary, collides with another drone, attacks an empty cell, or the algorithm does not destroy all weeds.',
            commandsTitle: 'How to write commands',
            commandsText: 'The command table consists of ticks. One tick is one algorithm step. Commands written in the same tick are executed in parallel.',
            tableTarget: 'Target',
            tableAction: 'Action',
            tableArgument: 'Argument',
            tableDescription: 'Description',
            targetAll: 'All',
            targetDrone1: 'Drone 1',
            targetDrone2: 'Drone 2',
            targetDrone3: 'Drone 3',
            actionForward: 'Forward',
            actionLeft: 'Left',
            actionRight: 'Right',
            actionAttack: 'Attack',
            emptyOrOne: 'empty or 1',
            descForward: 'All drones move forward the specified number of steps.',
            descLeft: 'The drone turns 90° to the left.',
            descRight: 'The drone turns 90° to the right.',
            descAttack: 'The drone destroys a weed in the cell directly in front of it.',
            step1Title: 'Choose a target',
            step1Text: 'You can choose a specific drone or the “All” command.',
            step2Title: 'Choose an action',
            step2Text: 'Movement, turns, and attack are available.',
            step3Title: 'Run the algorithm',
            step3Text: 'Click “Run” for full execution or “Run step by step” to execute one elementary action at a time.',
            parallelTitle: 'Parallel execution',
            parallelText: 'If one tick contains commands for several drones, they start executing at the same time. On maps with many drones, one tick may occupy several table rows.',
            parallelCode: 'Tick 1: Drone 1 — Forward    Drone 2 — Forward\nTick 1: Drone 3 — Attack     Drone 4 — Left\nTick 2: All     — Forward',
            parallelExample: 'In this example, the first four commands belong to one tick, while “All — Forward” is executed in the next step.',
            scoreTitle: 'Algorithm score',
            scoreText: 'After successful completion, the game displays the algorithm score.',
            formula: 'Score = left/right turns + “Forward” commands × 2',
            scoreHint: 'The “Attack” command does not increase the score. The lower the score, the shorter and more efficient the algorithm is.',
            mapsTitle: 'How to create maps',
            mapsText: 'Open <strong>File → Open map editor</strong>. In the editor, you can select cells and add or remove drones and weeds.',
            mapRule1: 'The map must contain at least one drone.',
            mapRule2: 'There cannot be more than 10 drones.',
            mapRule3: 'The number of weeds must not be less than the number of drones.',
            mapRule4: 'A drone and a weed cannot be placed in the same cell.',
            mapRule5: 'The map is saved to the <code>levels</code> folder in JSON format.',
            showMapJson: 'Show map JSON example',
            hideMapJson: 'Hide map JSON example',
            algorithmsTitle: 'How to save and load algorithms',
            algorithmsText: 'Commands from the table can be saved through <strong>File → Save algorithm</strong>. Algorithms are stored in the <code>algorithms</code> folder in JSON format.',
            algorithmRule1: 'Specify an algorithm name when saving.',
            algorithmRule2: 'When loading, choose a JSON file from the <code>algorithms</code> folder.',
            algorithmRule3: 'The algorithm stores the number of drones it was created for.',
            algorithmRule4: 'If the current map has a different number of drones, the algorithm will not load.',
            showAlgorithmJson: 'Show algorithm JSON example',
            hideAlgorithmJson: 'Hide algorithm JSON example',
            tipsTitle: 'Tips',
            tip1: '<strong>Plan direction.</strong> The “Attack” command affects the cell in front of the drone, so turns matter.',
            tip2: '<strong>Check ticks.</strong> If several drones must act simultaneously, put their commands into the same tick.',
            tip3: '<strong>Watch charges.</strong> The total number of drone charges equals the number of weeds, so extra attacks will cause an error.',
            tip4: '<strong>Use test algorithms.</strong> They help check successful completion, collision, boundary hit, and incomplete weed destruction.',
            toTopLabel: 'Back to top',
            languageButton: 'RU'
        }
    };

    let currentLanguage = localStorage.getItem('guide-language') || 'ru';

    function translate(language) {
        currentLanguage = language;
        localStorage.setItem('guide-language', language);
        document.documentElement.lang = language;

        const dictionary = translations[language];

        document.querySelectorAll('[data-i18n]').forEach((element) => {
            const key = element.getAttribute('data-i18n');
            if (!dictionary[key]) {
                return;
            }

            element.innerHTML = dictionary[key];
        });

        document.querySelectorAll('[data-i18n-aria]').forEach((element) => {
            const key = element.getAttribute('data-i18n-aria');
            if (dictionary[key]) {
                element.setAttribute('aria-label', dictionary[key]);
            }
        });

        document.querySelectorAll('.example-toggle').forEach((button) => {
            const block = document.getElementById(button.getAttribute('data-target'));
            const key = block && block.classList.contains('hidden')
                ? button.getAttribute('data-show-key')
                : button.getAttribute('data-hide-key');

            button.textContent = dictionary[key] || button.textContent;
        });

        const languageButton = document.getElementById('language-toggle');
        languageButton.textContent = dictionary.languageButton;
    }

    document.getElementById('language-toggle').addEventListener('click', () => {
        translate(currentLanguage === 'ru' ? 'en' : 'ru');
    });

    const toggles = document.querySelectorAll('.example-toggle');

    toggles.forEach((button) => {
        button.addEventListener('click', () => {
            const targetId = button.getAttribute('data-target');
            const block = document.getElementById(targetId);

            if (!block) {
                return;
            }

            block.classList.toggle('hidden');

            const dictionary = translations[currentLanguage];
            const key = block.classList.contains('hidden')
                ? button.getAttribute('data-show-key')
                : button.getAttribute('data-hide-key');

            button.textContent = dictionary[key];
        });
    });

    const toTop = document.querySelector('.to-top');

    window.addEventListener('scroll', () => {
        if (window.scrollY > 500) {
            toTop.classList.add('visible');
        } else {
            toTop.classList.remove('visible');
        }
    });

    toTop.addEventListener('click', () => {
        window.scrollTo({ top: 0, behavior: 'smooth' });
    });

    translate(currentLanguage);
})();
