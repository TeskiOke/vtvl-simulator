# 🎯 ФИНАЛЬНОЕ РЕШЕНИЕ — Пересмотр платформы
> Группа 03 | ДГТУ | Кейс 8.1 | Обновлено: 17.09.2026

---

## ❌ ПОЧЕМУ НЕ PYTHON (и ты прав)

Python — скриптовый язык. Его слабости для нашего проекта:

| Проблема | Суть |
|----------|------|
| Медленный | GIL (Global Interpreter Lock) — нет настоящего параллелизма |
| Не для игр | Pygame — устаревшая библиотека, не для 3D |
| Слабая типизация | Ошибки находишь только в рантайме |
| Нет компиляции | Падает в рантайме, а не при сборке |
| "Не серьёзный" | В играх, симуляторах, авиации — не Python |

> SpaceX, NASA используют Python только для скриптов/автоматизации.  
> Ядро симуляций — C++, C#, Fortran, Julia.

---

## ✅ СЕРЬЁЗНЫЕ ЯЗЫКИ ДЛЯ 3D СИМУЛЯТОРА

### Сравнение:

| Критерий | C# (Unity) | C# (Godot) | C++ (Unreal) | C++ (чистый) |
|----------|-----------|------------|-------------|--------------|
| Надёжность | ★★★★★ | ★★★★★ | ★★★★★ | ★★★★★ |
| 3D из коробки | ✅ Unity | ✅ Godot | ✅ Unreal | ❌ сами |
| Скорость разработки | ★★★★☆ | ★★★★★ | ★★★☆☆ | ★★☆☆☆ |
| Производительность | ★★★★☆ | ★★★★☆ | ★★★★★ | ★★★★★ |
| Кривая обучения | Средняя | Средняя | Крутая | Очень крутая |
| Размер установки | 5-15 ГБ | 80 МБ | 60-100 ГБ | ~1 ГБ |
| Готовых ракетных проектов | 132 C# репо | 12 GDScript | 0 | редко |
| Тесты | NUnit / xUnit | GUT | Catch2 | Catch2/GTest |
| Итог | 🥇 | 🥈 | ⚠️ слишком тяжело | ⚠️ нет 3D |

---

## 🏆 ПОБЕДИТЕЛЬ: **Unity + C#**

### Почему Unity + C#:

```
C# — это:
  ✅ Компилируемый язык (ошибки находишь при сборке, не в рантайме)
  ✅ Строгая типизация (int, float, Vector3 — всё явно)
  ✅ ООП — классы, интерфейсы, наследование
  ✅ Используется в: игры (Unity), Microsoft, Boeing, Airbus симуляторы
  ✅ Быстрый (JIT-компиляция, IL2CPP для сборки)
  ✅ Euler и RK4 на C# пишутся чище и надёжнее чем на Python

Unity — это:
  ✅ Профессиональный 3D движок (используется NASA для симуляций)
  ✅ PhysX физический движок встроен
  ✅ Частицы, огонь, дым — из коробки
  ✅ Полноценный HUD через Canvas
  ✅ NUnit тесты встроены
  ✅ Экспорт: Windows, Mac, Linux, Web, Android
  ✅ Огромное сообщество, документация на русском
```

### Важно: КАК использовать физику в Unity

```
❌ НЕ берём Unity Rigidbody для расчётов
✅ ПИШЕМ свой Euler/RK4 на C#

Причина: кейс требует ЯВНОГО численного интегрирования.
Unity Rigidbody это делает внутри (PhysX), мы не видим шагов.

Наш подход:
  - C# класс RocketPhysics: считает Euler/RK4
  - Unity: только рендер (позиция ракеты = output нашей физики)
  - Это называется: кастомный физический движок поверх рендерера
```

---

## 🚀 АРХИТЕКТУРА ПРОЕКТА (Unity + C#)

```
rocket_simulator_unity/
├── Assets/
│   ├── Scripts/
│   │   ├── Physics/
│   │   │   ├── RocketState.cs        // struct: h, v, m, t
│   │   │   ├── Forces.cs             // тяга, гравитация, Cd, ветер
│   │   │   ├── Atmosphere.cs         // ISA: ρ(h), T(h), P(h)
│   │   │   ├── Weather.cs            // ветер, порывы, турбулентность
│   │   │   ├── EulerIntegrator.cs    // Метод Эйлера
│   │   │   └── RK4Integrator.cs      // Метод Рунге-Кутты 4
│   │   │
│   │   ├── Game/
│   │   │   ├── GameLoop.cs           // фиксированный физ. шаг (FixedUpdate)
│   │   │   ├── LandingDetector.cs    // посадка v<2, крушение v>5
│   │   │   ├── TelemetryLogger.cs    // CSV запись
│   │   │   └── DifficultyConfig.cs   // Easy/Normal/Hard ScriptableObject
│   │   │
│   │   ├── UI/
│   │   │   ├── HUDController.cs      // h, v, топливо%, TWR, n
│   │   │   ├── ThrottleSlider.cs     // ползунок тяги (0-100%)
│   │   │   ├── TutorialManager.cs    // 5 шагов туториала
│   │   │   └── PauseMenu.cs          // пауза, рестарт
│   │   │
│   │   └── Input/
│   │       └── InputController.cs    // клавиши ↑↓, ползунок
│   │
│   ├── Tests/                         // NUnit / Unity Test Framework
│   │   ├── EulerTests.cs             // 5+ тестов Эйлера
│   │   ├── RK4Tests.cs               // 5+ тестов RK4
│   │   ├── AtmosphereTests.cs        // тесты ISA
│   │   ├── ForcesTests.cs            // тесты сил
│   │   └── LandingTests.cs           // тесты посадки
│   │
│   ├── Scenes/
│   │   ├── MainMenu.unity
│   │   ├── Simulation.unity           // основная сцена
│   │   └── Tutorial.unity
│   │
│   ├── Prefabs/
│   │   ├── Rocket.prefab              // 3D модель ракеты
│   │   ├── LaunchPad.prefab           // посадочная площадка
│   │   ├── EngineParticles.prefab     // огонь двигателя
│   │   └── ExplosionParticles.prefab  // взрыв при крушении
│   │
│   └── Resources/
│       ├── Configs/
│       │   ├── EasyDifficulty.asset
│       │   ├── NormalDifficulty.asset
│       │   └── HardDifficulty.asset
│       └── RocketParams/
│           └── Falcon9.asset          // реальные параметры Falcon 9
│
└── README.md
```

---

## 💻 КОД НА C# — примеры (как это выглядит)

### RocketState.cs
```csharp
public struct RocketState
{
    // === Позиция и движение (3D) ===
    public Vector3 Position;        // позиция [x, y, z] (м), y = высота
    public Vector3 Velocity;        // скорость [v_x, v_y, v_z] (м/с)
    
    // === Ориентация ===
    public Quaternion Rotation;     // кватернион ориентации (тангаж, рыскание, крен)
    public Vector3 AngularVelocity; // угловая скорость [ω_x, ω_y, ω_z] (рад/с)
    
    // === Масса ===
    public float Mass;              // полная масса (кг) = m_dry + m_fuel
    public float FuelMass;          // остаток топлива (кг)
    
    // === Время ===
    public float Time;              // время симуляции (с)
    
    // === Удобные свойства ===
    public float Height => Position.y;
    public float VerticalSpeed => Velocity.y;
    public float HorizontalSpeed => new Vector2(Velocity.x, Velocity.z).magnitude;
}
```

### EulerIntegrator.cs
```csharp
public static class EulerIntegrator
{
    public static RocketState Step(RocketState state, float dt, 
                                   Func<RocketState, (Vector3 velocity, Vector3 acceleration, float dMass)> derivative)
    {
        var (v, a, dm) = derivative(state);
        return new RocketState
        {
            Position = state.Position + v * dt,
            Velocity = state.Velocity + a * dt,
            Rotation = state.Rotation,
            AngularVelocity = state.AngularVelocity,
            Mass     = Mathf.Max(state.Mass + dm * dt, 25000f),
            FuelMass = Mathf.Max(state.FuelMass + dm * dt, 0f),
            Time     = state.Time + dt
        };
    }
}
```

### RK4Integrator.cs
```csharp
public static class RK4Integrator
{
    public static RocketState Step(RocketState state, float dt,
                                   Func<RocketState, (Vector3 v, Vector3 a, float dm)> f)
    {
        var k1 = f(state);
        var k2 = f(Add(state, Scale(k1, dt * 0.5f)));
        var k3 = f(Add(state, Scale(k2, dt * 0.5f)));
        var k4 = f(Add(state, Scale(k3, dt)));
        
        Vector3 dPos = (dt / 6f) * (k1.v + 2f * k2.v + 2f * k3.v + k4.v);
        Vector3 dVel = (dt / 6f) * (k1.a + 2f * k2.a + 2f * k3.a + k4.a);
        float   dM   = (dt / 6f) * (k1.dm + 2f * k2.dm + 2f * k3.dm + k4.dm);

        return new RocketState
        {
            Position = state.Position + dPos,
            Velocity = state.Velocity + dVel,
            Rotation = state.Rotation,
            AngularVelocity = state.AngularVelocity,
            Mass     = Mathf.Max(state.Mass + dM, 25000f),
            FuelMass = Mathf.Max(state.FuelMass + dM, 0f),
            Time     = state.Time + dt
        };
    }
}
```

### Atmosphere.cs (ISA)
```csharp
public static class Atmosphere
{
    public static (float rho, float T, float P) ISA(float altitude)
    {
        float h = Mathf.Max(0f, altitude);
        if (h < 11000f)  // тропосфера
        {
            float T = 288.15f - 0.0065f * h;
            float P = 101325f * Mathf.Pow(T / 288.15f, 5.2561f);
            float rho = P / (287.05f * T);
            return (rho, T, P);
        }
        else  // стратосфера
        {
            float T = 216.65f;
            float P = 22632f * Mathf.Exp(-0.0001577f * (h - 11000f));
            float rho = P / (287.05f * T);
            return (rho, T, P);
        }
    }
}
```

---

## ⚙️ ФИЗИКА (3D — полный симулятор)

```
Оси координат Unity (Y = вверх):
  Вертикаль: Y  — высота, скорость снижения
  Горизонталь: X и Z — боковой ветер, дрейф

Силы:
  F_thrust  = throttle × F_max × Vector3.up  (управление игроком)
  F_gravity = mass × g(h) × Vector3.down          (переменная: g(h) = 9.80665 × (6371000 / (6371000 + h))²)
  F_drag    = 0.5 × Cd × ρ(h) × A × v² × -v.normalized  (всегда против движения)
  F_wind    = 0.5 × Cd × ρ(h) × A_side × W(t)² × wind_dir  (случайный вектор)
```

---

## 🎮 ТРИ УРОВНЯ СЛОЖНОСТИ

| Параметр | Easy | Normal | Hard |
|----------|------|--------|------|
| Топливо | ∞ | 40 000 кг | 25 000 кг |
| Атмосфера | Нет сопротивления | ISA ρ(h) | ISA + турбулентность |
| Ветер | Нет | Нет | Полная 3D модель |
| Высота старта | 500 м | 5 000 м | 20 000 м |
| Нач. скорость | -20 м/с | -100 м/с | -250 м/с |
| Победа | v_y ≤ 3 м/с, θ ≤ 10° | v_y ≤ 2 м/с, v_h ≤ 0.5, θ ≤ 5° | v_y ≤ 1.5 м/с, v_h ≤ 0.3, θ ≤ 3°, R ≤ 15 м |
| Грубая посадка | 3–5 м/с | 2–5 м/с | 1.5–5 м/с |
| Крушение | v_y > 5 м/с или θ > 15° или n > 6G | v_y > 5 м/с или θ > 15° или n > 6G | v_y > 5 м/с или θ > 15° или n > 6G |
| TWR при старте | ~2.5 | ~1.4 | ~1.1 |

---

## 📅 ПЛАН НА 3 МЕСЯЦА (Unity + C#)

### Месяц 1 — Основа
| Задача | Кто | Срок |
|--------|-----|------|
| Установить Unity LTS (2022.3) | ИТ (5 чел.) | 1-2 дня |
| Изучить C# основы (типы, классы, методы) | ИТ | 1 неделя |
| Матмодель, параметры Falcon 9 | КР | 2 недели |
| Написать RocketState, Euler, RK4 в C# | ИТ + КР | 2 недели |
| Unit тесты на NUnit | ИТ | 1 неделя |

### Месяц 2 — Игра
| Задача | Кто | Срок |
|--------|-----|------|
| 3D сцена: ракета, площадка, камера | ИТ | 1 неделя |
| Подключить физику к Unity рендеру | ИТ | 1 неделя |
| HUD (Canvas): высота, скорость, топливо, TWR | ИТ + ИСС | 1 неделя |
| Туториал 5 шагов, Easy/Normal уровни | ИТ + ИСС | 1 неделя |

### Месяц 3 — Полировка
| Задача | Кто | Срок |
|--------|-----|------|
| Hard уровень: ветер, 3D дрейф | ИТ + КР | 1 неделя |
| Частицы: огонь, дым, взрыв | ИТ | 1 неделя |
| Анализ Euler vs RK4, отчёт | КР + ИК | 1 неделя |
| 10 тест-кейсов, демо, презентация | Все | 1 неделя |

---

## 🔑 ЧТО БЕРЁМ ИЗ НАЙДЕННЫХ ПРОЕКТОВ

| Проект | Что берём | Ссылка |
|--------|----------|--------|
| `alxndrTL/Landing-Starships` (Unity, ⭐133) | 3D модель Starship, эффекты частиц, логику посадки | https://github.com/alxndrTL/Landing-Starships |
| `rsewell97/open-starship` (Python) | Параметры Falcon 9 (масса, тяга, Isp, Cd) — только данные | https://github.com/rsewell97/open-starship |
| `arasgungore/rocket-flight-simulator` (MATLAB ⭐40) | Матмодель, графики h(t) v(t) — для КР задача 2-3 | https://github.com/arasgungore/rocket-flight-simulator |

---

## ✅ ИТОГ (базовый)

```
Язык:       C# (строгий, компилируемый, надёжный)
Движок:     Unity 2022.3 LTS (3D, PhysX, Canvas HUD, частицы)
Физика:     Свой Euler + RK4 на C# (НЕ Rigidbody для расчётов)
3D:         Да — полноценная 3D сцена
Атмосфера:  ISA (ρ(h), T(h), P(h)) — обязательно
Ветер:      3D вектор на уровне Hard
Уровни:     Easy / Normal / Hard
Тесты:      Unity Test Framework (NUnit)
CSV:        System.IO.StreamWriter — встроено в C#
Дедлайн:    3 месяца — реально и с запасом
```

---

## 🤖 НОВЫЕ ФИЧИ (предложения команды)

---

### Фича 1: АВТОПИЛОТ + Ручное управление (переключение в любой момент)

#### Концепция:
```
[TAB] или кнопка → переключить режим
  MANUAL MODE:  игрок управляет тягой сам (клавиши / ползунок)
  AUTO MODE:    автопилот управляет тягой по алгоритму

Переключение работает В ЛЮБОЙ МОМЕНТ полёта:
  - Включил AUTO → смотришь как ракета садится сама
  - Перехватил MANUAL → сам доводишь
  - Снова AUTO → он продолжает с текущего состояния
```

#### Как работает автопилот (алгоритм):

**Алгоритм "Suicide Burn" (именно то что использует Falcon 9):**
```
Идея: в каждый момент времени вычисляем —
  "если начать тормозить ПРЯМО СЕЙЧАС на полной тяге,
   успеем ли остановиться до земли?"

Математика:
  h_brake = v² / (2 × a_brake)    ← минимальная высота для торможения (базовая оценка)
  a_brake = (F_max/m(t) - g(h))    ← ускорение торможения

  ВАЖНО: масса m(t) меняется во время торможения!
  dm/dt = -T / (Isp × g0)         ← ракета сжигает топливо и становится легче
  → a_brake растёт со временем, точное решение требует итерации или ODE

  Мин. дроссель: 40% (Merlin 1D: 40-100%). Зависание невозможно!

  if (h <= h_brake × 1.05) → полная тяга (100%)
  else                    → минимальная тяга (40%) или 0% (выключен)

Это нелинейная система → решается итерационно на каждом шаге физики
```

**Алгоритм для горизонтального наведения (из точки C в B):**
```
Horizontal PID:
  error_x = target.x - rocket.x    ← отклонение по X
  error_z = target.z - rocket.z    ← отклонение по Z

  thrust_angle_x = Kp × error_x + Kd × vel.x  ← PID
  thrust_angle_z = Kp × error_z + Kd × vel.z

  → гимбал двигателя отклоняется на этот угол
  → ракета летит к цели по горизонтали
```

#### Код на C# (AutopilotController.cs):
```csharp
public class AutopilotController : MonoBehaviour
{
    public bool IsActive { get; private set; }
    
    [Header("PID gains")]
    public float Kp = 0.05f;
    public float Kd = 0.3f;
    
    // Целевая точка посадки
    public Vector3 TargetLandingPoint;
    
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
            IsActive = !IsActive;  // переключение в любой момент
    }
    
    // Возвращает команду тяги [0..1] и угол гимбала
    public (float throttle, Vector3 gimbalAngle) ComputeControl(RocketState state)
    {
        if (!IsActive) return (0f, Vector3.zero);  // ручной режим
        
        // === ВЕРТИКАЛЬ: Suicide Burn ===
        float a_brake = (rocketParams.F_max / state.Mass) - Physics.gravity.magnitude;
        float h_brake = (state.VerticalSpeed * state.VerticalSpeed) / (2f * a_brake);
        
        float throttle = (state.Height <= h_brake * 1.05f) ? 1.0f : 0.0f; // мин. дроссель 40%, но вне зоны торможения двигатель выключен
        
        // === ГОРИЗОНТАЛЬ: PID к цели ===
        Vector3 horizontalError = TargetLandingPoint - rocketPosition;
        horizontalError.y = 0;  // только горизонтальная составляющая
        
        Vector3 gimbal = new Vector3(
            Kp * horizontalError.x - Kd * horizontalVelocity.x,
            0,
            Kp * horizontalError.z - Kd * horizontalVelocity.z
        );
        gimbal = Vector3.ClampMagnitude(gimbal, maxGimbalAngle);
        
        return (throttle, gimbal);
    }
}
```

---

### Фича 2: МИССИЯ ТОЧКА→ТОЧКА (A → C → B)

#### Концепция:
```
Пользователь на карте выбирает:
  📍 A = Точка взлёта (стартовая площадка, фиксированная)
  📍 B = Точка посадки (цель, выбирается на карте)
  📍 C = Промежуточная точка / апогей / маршрутная точка

Сценарий: Falcon 9 First Stage
  A → Взлёт → C (апогей 60 км) → разворот → B (посадочная зона)
  
Автопилот считает математически:
  1. Импульс на взлёт (delta-V вверх + горизонтально)
  2. Баллистическую траекторию через C
  3. Тормозной импульс для попадания в B
  4. Финальный suicide burn для мягкой посадки
```

#### Математика траектории (C → B):

```
Задача: из точки C (h_c, x_c) попасть в B (0, x_b) с v_land < 2 м/с
        при ограниченном топливе и максимальной тяге F_max

Метод: Итерационный численный счёт с прогнозом

Алгоритм решения (каждые 0.5 с пересчёт):
  1. Симулируем траекторию вперёд с текущего состояния
     (быстрая симуляция, не рендер — "теневая физика")
  2. Где ракета приземлится при текущей тяге?
  3. Разница с точкой B → корректируем угол тяги
  4. Повторяем до точности < 10 м

В коде (C# async):
  Task<ThrustProfile> ComputeTrajectory(RocketState current, Vector3 target)
  {
      // Запускаем симуляцию в отдельном потоке
      // Не блокирует игровой цикл
      // Каждые 0.5 с возвращает обновлённый план тяги
  }
```

#### UI выбора точек (на 3D карте Unity):
```
Карта сверху (Top-down вид):
  [Click] → поставить маркер B (зелёная зона посадки)
  [Shift+Click] → поставить маркер C (промежуточная точка)
  [Enter] → рассчитать траекторию

Отображение:
  Синяя дуга = рассчитанная траектория
  Красные точки = критические точки (торможение, апогей)
  Время полёта = отображается до старта
  Расход топлива = расчётный
```

---

### Фича 3: ВИЗУАЛИЗАЦИЯ ТРАЕКТОРИИ (предсказание пути)

```
Пока летим — рисуем:
  🔵 Синяя пунктирная линия = предсказанный путь (теневая физика вперёд на 30 с)
  🟡 Жёлтая = плановая траектория автопилота
  🔴 Красная точка = предсказанное место падения (если тяга = 0)

Как в KSP — игрок видит куда летит заранее
```

---

## 📊 ОБНОВЛЁННАЯ АРХИТЕКТУРА (с новыми фичами)

```
Assets/Scripts/
├── Physics/
│   ├── RocketState.cs
│   ├── Forces.cs
│   ├── Atmosphere.cs          ISA
│   ├── Weather.cs             3D ветер
│   ├── EulerIntegrator.cs
│   └── RK4Integrator.cs
│
├── Autopilot/                 ← НОВОЕ
│   ├── AutopilotController.cs  переключение AUTO/MANUAL
│   ├── SuicideBurnGuidance.cs  алгоритм торможения
│   ├── HorizontalPID.cs        горизонтальное наведение
│   └── TrajectoryPlanner.cs    расчёт траектории C→B
│
├── Mission/                   ← НОВОЕ
│   ├── MissionConfig.cs        точки A, B, C
│   ├── MissionPlanner.cs       выбор точек на карте
│   └── TrajectoryPredictor.cs  теневая физика (предсказание)
│
├── Game/
│   ├── GameLoop.cs
│   ├── InputController.cs     MANUAL + AUTO переключение
│   ├── LandingDetector.cs
│   ├── TelemetryLogger.cs
│   └── DifficultyConfig.cs
│
└── UI/
    ├── HUDController.cs        + индикатор AUTO/MANUAL
    ├── MissionMapUI.cs         ← НОВОЕ выбор точек на карте
    ├── TrajectoryRenderer.cs   ← НОВОЕ синяя линия пути
    ├── TutorialManager.cs
    └── PauseMenu.cs
```

---

## 🌌 ФИЧА 4: ПОЛНЫЙ ПРОФИЛЬ ПОЛЁТА В КОСМОС И ВОЗВРАЩЕНИЕ (SUBORBITAL REENTRY)

> **Идея:** Не просто взлёт и посадка «в огороде», а полноценный суборбитальный космический полёт по профилю **New Shepard / Starship / Falcon 9 RTLS**:
> Взлёт из точки А ➔ Гравитационный манёвр ➔ Выход в космос (>100 км, Линия Кармана) ➔ Космический полёт / апогей ➔ Вход в плотные слои атмосферы (Reentry) ➔ Аэродинамическое торможение ➔ Прицельный реверс двигателей (Suicide Burn) в точке Б!

---

### 🚀 Фазы миссии (Mission Flight Phases)

```
        [Фаза 3: АПОГЕЙ В КОСМОСЕ (h > 100 км, вакуум)]
                       ★ Точка C (Апогей)
                     /                    \
     [Фаза 2: Выход]                       [Фаза 4: Вход в атмосферу]
      Гравитационный разворот                Плазменный след, нагрев
      Разгон под углом                       Аэродинамический тормоз
           /                                       \
   [Фаза 1: Взлёт]                                  [Фаза 5: Посадочный манёвр]
    Старт со стола                                   Flip maneuver (Starship) /
    Вертикальный набор                               Suicide burn двигателями
       /                                                   \
  Точка A (Космодром)                                 Точка B (Платформа / Баржа)
```

| Фаза | Высота ($h$) | Физика и эффекты | Роль автопилота |
|------|-------------|-------------------|-----------------|
| **1. Liftoff (Взлёт)** | 0 – 10 км | Максимальная плотность воздуха, $Max\ Q$, преодоление звукового барьера. | Удержание вертикали, компенсация приземного ветра. |
| **2. Gravity Turn (Разворот)** | 10 – 70 км | Плотность $\rho(h)$ падает экспоненциально. Наклон вектора тяги в сторону точки B. | Программный наклон угла тангажа (Pitch Program). |
| **3. Exoatmospheric Coast (Космос)** | 70 – 120+ км | Линия Кармана (100 км). Полный вакуум ($\rho = 0$). Аэродинамика отключается, работают только маневровые RCS-сопла. | Ориентация ступени кормой/носом по вектору входа (Retrograde). |
| **4. Atmospheric Reentry (Вход)** | 100 – 20 км | Гиперзвуковой вход в атмосферу. Резкий рост скоростного напора $q = \frac{1}{2}\rho v^2$. Эффект нагрева корпуса (визуальное свечение плазмы). | Стабилизация решётчатыми рулями (Grid Fins), коррекция точки падения. |
| **5. Terminal Suicide Burn (Посадка)** | 20 – 0 км | Дозвуковое торможение, включение маршевого двигателя в точке невозврата ($t_{burn}$). | Точный расчёт момента зажигания и прецизионное касание в точке B ($v_y < 1$ м/с). |

---

### 🧮 Дополнительная физика для космического профиля

1. **Закон всемирного тяготения (переменная гравитация с высотой):**
   $$g(h) = g_0 \cdot \left(\frac{R_E}{R_E + h}\right)^2$$
   - $R_E = 6\,371\,000$ м (радиус Земли)
   - $g_0 = 9.80665$ м/с²
   - На высоте 100 км $g \approx 9.51$ м/с² (гравитация ослабевает, нельзя считать $g$ константой!)

2. **Модель атмосферы до 100+ км (US Standard Atmosphere / ISA):**
   - **Тропосфера (0–11 км):** $T(h) = T_0 - L \cdot h$
   - **Стратосфера (11–47 км):** Изотермический слой и нагрев от озона
   - **Мезосфера (47–86 км):** Падение температуры до $-90^\circ\text{C}$
   - **Термосфера / Вакуум (>86–100 км):** $\rho \to 0$, лобовое сопротивление $F_{drag} \to 0$

3. **Скоростной напор и плазменный нагрев (визуальный эффект в Unity):**
   $$q = \frac{1}{2} \cdot \rho(h) \cdot v^2$$
   - При $q > 50\text{ кПа}$ и $v > 1000\text{ м/с}$ Unity-шейдер активирует эффект плазменного свечения (Reentry Fire Effect).

4. **Аэродинамические рули (Grid Fins / Body Flaps):**
   - В космосе не работают (нет воздуха).
   - В плотных слоях создают управляющий момент:
     $$\vec{M}_{aero} = \frac{1}{2} C_L \rho v^2 S_{fin} \cdot L_{arm}$$

---

## 🔥 ЧТО ЭТО ДАЁТ ПРОЕКТУ

| Фича | Ценность для кейса | Уникальность |
|------|-------------------|--------------|
| **Выход в космос + Reentry** | Полноценный космический профиль уровня Kerbal Space Program / SpaceX | ★★★★★ |
| **AUTO / MANUAL переключение** | Интерактивность: игрок может перехватить управление на любой фазе (TAB) | ★★★★★ |
| **Точки A ➔ C (Апогей) ➔ B (Посадка)** | Свобода планирования миссий и расчет оптимального импульса схода | ★★★★★ |
| **Suicide Burn + Gravity Turn** | Реальная астродинамика и системы наведения (Guidance, Navigation & Control) | ★★★★★ |
| **Предиктивная траектория** | Отображение расчётной траектории падения (Impact Point Predictor) в реальном времени | ★★★★☆ |

> Это превращает кейс из простой школьной лабораторной в мощный симулятор космических запусков аэрокосмического уровня!

---

## ✅ ИТОГОВАЯ АРХИТЕКТУРА И ПЛАН РЕАЛИЗАЦИИ

```
Язык:        C# (.NET / Unity LTS) — строгая типизация, максимальная производительность
Движок:      Unity 2022.3 LTS (3D визуал, шейдеры плазмы, частицы двигателей, HUD)
Физика:      Кастомный 3D численный интегратор (Euler + Runge-Kutta 4) на C#
             * Не зависит от встроенного PhysX — 100% научная достоверность
             * Вектор состояния: Position(x,y,z), Velocity(v_x,v_y,v_z), Rotation(кватернион), AngularVelocity, Mass, FuelMass
             * 1D-режим для верификации с эталоном Excel (КР-7)
Гравитация:  Закон Ньютона $g(h) = g_0 \cdot (R_E / (R_E+h))^2$
Атмосфера:   ISA модель (0 – 120 км) с термодинамическими слоями
Управление:  Двойное:
             - MANUAL: WASD (наклон сопла), Shift/Ctrl (дроссель тяги), Space (RCS)
             - AUTO: Автопилот с фазовым автоматом (Ascent -> Coast -> Reentry -> Suicide Burn)
Миссия:      Выбор на 3D карте: Космодром A, Целевой Апогей C, Посадочная зона B
Телеметрия:  Экспорт всех параметров в CSV (высота, скорость, масса топлива, температура, ускорение)
```

---

## 🚦 ПЛАН ДАЛЬНЕЙШИХ ДЕЙСТВИЙ

1. **Шаг 1: Математическое ядро на чистом C# (`CorePhysics`)**
   - Написание классов `Vector3d` (двойная точность для орбитальных высот), `AtmosphereModel` (ISA 0–120 км), `RocketState`, `RK4Integrator`.
   - Написание юнит-тестов (NUnit) для проверки формулы Циолковского, баллистики и посадочного импульса.

2. **Шаг 2: Алгоритмы автопилота (`Guidance & Control`)**
   - Реализация расчёта момента торможения `SuicideBurnCalculator`.
   - Контроллер угла атаки при входе в атмосферу и наведение на координаты точки B.

3. **Шаг 3: Unity 3D проект и интеграция визуала**
   - Настройка сцены (земля, атмосфера со скайбоксом космической глубины, звёзды).
   - 3D модель ракеты (Starship / Falcon 9) с анимированным вектором сопла (Gimbal) и частицами пламени/RCS.
   - HUD с переключателем AUTO/MANUAL, спидометром, высотомером и динамической траекторией.


