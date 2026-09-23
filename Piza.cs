using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

enum PizzaStatus { Cooking, Ready }
enum OrderStatus { Accepted, Cooking, Ready, PickedUp }

class User
{
    public Guid Id { get; } = Guid.NewGuid();
    public string Name { get; }
    public User(string name) => Name = name;
}

class Pizza
{
    public Guid Id { get; } = Guid.NewGuid();
    public string Name { get; }
    public PizzaStatus Status { get; private set; } = PizzaStatus.Cooking;

    public Pizza(string name) => Name = name;
    public void MarkReady() => Status = PizzaStatus.Ready;
}

class Order
{
    public Guid Id { get; } = Guid.NewGuid();
    public User User { get; }
    public List<Pizza> Pizzas { get; }
    public OrderStatus Status { get; private set; } = OrderStatus.Accepted;

    public Order(User user, IEnumerable<Pizza> pizzas)
    {
        User = user;
        Pizzas = pizzas.ToList();
    }

    public void SetStatus(OrderStatus s) => Status = s;
}

class DisplayBoard
{
    private readonly List<string> _ready = new();

    public void Show(string userName)
    {
        _ready.Add(userName);
        Console.WriteLine();
        Console.WriteLine("==============================");
        Console.WriteLine($"   ТАБЛО: {userName} — ЗАКАЗ ГОТОВ!");
        Console.WriteLine("==============================");
    }

    public bool IsShown(string name) => _ready.Contains(name);
}

class Pizzeria
{
    private readonly DisplayBoard _board;
    private readonly List<Order> _orders = new();

    // меню пиццерии
    private readonly string[] _menu = { "Маргарита", "Пепперони", "Гавайская", "4 сыра" };

    public Pizzeria(DisplayBoard board) => _board = board;

    public string[] Menu => _menu;

    public Order AcceptOrder(User user, IEnumerable<string> pizzaNames)
    {
        var pizzas = pizzaNames.Select(n => new Pizza(n));
        var order = new Order(user, pizzas);
        _orders.Add(order);

        Console.WriteLine($"[Касса] Приняли заказ от {user.Name}: {string.Join(", ", pizzaNames)}");
        Console.WriteLine($"[Касса] Номер заказа: {order.Id.ToString()[..4]}");
        return order;
    }

    public async Task CookAsync(Order order, int msPerPizza = 900)
    {
        order.SetStatus(OrderStatus.Cooking);
        Console.WriteLine("[Кухня] Начали готовить...");

        foreach (var pizza in order.Pizzas)
        {
            await Task.Delay(msPerPizza);
            pizza.MarkReady();
            Console.WriteLine($"[Кухня] {pizza.Name} готова");
        }

        order.SetStatus(OrderStatus.Ready);
        _board.Show(order.User.Name);
    }

    public void PickUp(Order order)
    {
        if (order.Status != OrderStatus.Ready)
            throw new InvalidOperationException("Заказ ещё не готов");

        order.SetStatus(OrderStatus.PickedUp);
        Console.WriteLine($"[Выдача] {order.User.Name} забрал заказ. Приятного аппетита!");
    }
}

class Program
{
    static async Task Main()
    {
        var board = new DisplayBoard();
        var pizzeria = new Pizzeria(board);

        Console.WriteLine("=== Добро пожаловать в пиццерию ===");
        Console.Write("Как вас зовут? ");
        var name = Console.ReadLine()?.Trim();
        if (string.IsNullOrEmpty(name)) name = "Гость";

        var user = new User(name);

        // показываем меню
        Console.WriteLine("\nМеню:");
        for (int i = 0; i < pizzeria.Menu.Length; i++)
            Console.WriteLine($"  {i + 1}. {pizzeria.Menu[i]}");

        Console.Write("\nВыберите пиццы через запятую (например: 1,3): ");
        var input = Console.ReadLine() ?? "";
        var chosen = input
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(s => s.Trim())
            .Where(s => int.TryParse(s, out _))
            .Select(s => int.Parse(s) - 1)
            .Where(i => i >= 0 && i < pizzeria.Menu.Length)
            .Select(i => pizzeria.Menu[i])
            .ToList();

        if (chosen.Count == 0)
        {
            Console.WriteLine("Ничего не выбрано, берём Маргариту по умолчанию.");
            chosen.Add(pizzeria.Menu[0]);
        }

        // юзер делает заказ
        var order = pizzeria.AcceptOrder(user, chosen);

        // юзер садится ждать, кухня параллельно готовит
        Console.WriteLine("\nВы сели в зале и ждёте своё имя на табло...");
        var cookTask = pizzeria.CookAsync(order);

        // ждём, пока на табло не появится имя (юзер "смотрит" на табло)
        while (!board.IsShown(user.Name))
        {
            await Task.Delay(200);
        }

        Console.WriteLine("\nВы увидели своё имя и пошли к стойке выдачи.");
        Console.Write("Нажмите Enter, чтобы забрать заказ...");
        Console.ReadLine();

        await cookTask;
        pizzeria.PickUp(order);
    }
}