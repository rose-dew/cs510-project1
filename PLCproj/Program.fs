module Server

open System
open System.IO
open System.Threading.Tasks
open System.Net
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Logging
open Giraffe
open Giraffe.ViewEngine
open Giraffe.EndpointRouting
open System.Net.Http
open System.Net.Mail
open Microsoft.Extensions.Configuration
open System.Text.Json
open Microsoft.Extensions.Hosting

// Domain types
type TodoItem = { Id: int; Text: string; IsCompleted: bool }
type WeatherData = { Temperature: float; WeatherCode: int; Description: string }
type AppState = { Todos: TodoItem list; Weather: WeatherData option; ImageUrl: string option }

// Configuration
let config = 
    let env = 
        Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
        |> Option.ofObj
        |> Option.defaultValue "Development"

    ConfigurationBuilder()
        .SetBasePath(Directory.GetCurrentDirectory())
        .AddJsonFile("config.json", optional = false, reloadOnChange = true)
        .AddJsonFile($"config.{env}.json", optional = true)
        .Build()

// Email service
let sendEmail (subject: string) (body: string) =
    task {
        try
            use client = new SmtpClient()
            client.Host <- config["Email:SmtpServer"]
            client.Port <- config["Email:Port"] |> int
            client.EnableSsl <- config["Email:EnableSsl"] |> bool.Parse
            client.UseDefaultCredentials <- false
            client.Credentials <- System.Net.NetworkCredential(config["Email:Username"],config["Password"])
            client.DeliveryMethod <- SmtpDeliveryMethod.Network
            
            use mail = new MailMessage()
            mail.From <- MailAddress(config["Email:FromAddress"])
            mail.To.Add(config["Email:ToAddress"])
            mail.Subject <- subject
            mail.Body <- body
            mail.IsBodyHtml <- false
            
            do! client.SendMailAsync(mail)
            return Ok "Email sent"
        with ex ->
            return Error $"Failed to send email: {ex.Message}"
    }

// Weather service
let getWeatherData () =
    task {
        use client = new HttpClient()
        let lat = config["Weather:Latitude"]
        let lon = config["Weather:Longitude"]
        let url = sprintf "https://api.open-meteo.com/v1/forecast?latitude=%s&longitude=%s&current_weather=true" lat lon
        
        try
            let! response = client.GetAsync(url)
            let! content = response.Content.ReadAsStringAsync()
            
            let jsonDoc = JsonDocument.Parse(content)
            let root = jsonDoc.RootElement
            let currentWeather = root.GetProperty("current_weather")
            
            let temperature = currentWeather.GetProperty("temperature").GetDouble()
            let weatherCode = currentWeather.GetProperty("weathercode").GetInt32()
            
            // Simple weather code to description mapping
            let description =
                match weatherCode with
                | 0 -> "Clear sky"
                | 1 | 2 | 3 -> "Mainly clear, partly cloudy"
                | 45 | 48 -> "Fog"
                | 51 | 53 | 55 -> "Drizzle"
                | 56 | 57 -> "Freezing drizzle"
                | 61 | 63 | 65 -> "Rain"
                | 66 | 67 -> "Freezing rain"
                | 71 | 73 | 75 -> "Snow"
                | 77 -> "Snow grains"
                | 80 | 81 | 82 -> "Rain showers"
                | 85 | 86 -> "Snow showers"
                | 95 | 96 | 99 -> "Thunderstorm"
                | _ -> "Unknown weather"
            
            return Some { Temperature = temperature; WeatherCode = weatherCode; Description = description }
        with _ ->
            return None
    }

// Image service
let getRandomImage () =
    task {
        use client = new HttpClient()
        client.DefaultRequestHeaders.Add("Authorization", sprintf "Client-ID %s" config["Unsplash:AccessKey"])
        let url = "https://api.unsplash.com/photos/random?query=nature"
        
        try
            let! response = client.GetAsync(url)
            let! content = response.Content.ReadAsStringAsync()
            
            let jsonDoc = JsonDocument.Parse(content)
            let root = jsonDoc.RootElement
            let urls = root.GetProperty("urls")
            let imageUrl = urls.GetProperty("regular").GetString()
            
            return Some imageUrl
        with _ ->
            return None
    }

// State management
let mutable appState = { Todos = []; Weather = None; ImageUrl = None }

// Views
let layout (content: XmlNode list) =
    html [] [
        head [] [
            title [] [ str "Todo Weather App" ]
            link [ _rel "stylesheet"; _href "https://cdn.jsdelivr.net/npm/bootstrap@5.1.3/dist/css/bootstrap.min.css" ]
            script [ _src "https://unpkg.com/htmx.org@1.9.2" ] []
        ]
        body [] [
            div [ _class "container mt-4" ] content
        ]
    ]

let todoAddForm () =
    div [ _class "mb-3" ] [
        form [ 
            _method "post" 
            attr "hx-post" "/todos" 
            attr "hx-swap" "outerHTML"
            attr "hx-target" "#todo-list-container"  // Target only the list
            attr "hx-on:after-request" "this.reset()"
        ] [
            div [ _class "input-group" ] [
                input [ 
                    _type "text" 
                    _name "text" 
                    _class "form-control" 
                    _placeholder "Add new todo" 
                    _autocomplete "off"
                ]
                button [ 
                    _type "submit" 
                    _class "btn btn-primary" 
                ] [ str "Add" ]
            ]
        ]
    ]

let todoListView (todos: TodoItem list) =
    div [ _id "todo-list-container" ] [  // Add ID for targeting
        ul [ _class "list-group" ] [
            for todo in todos do
                li [ _class "list-group-item d-flex justify-content-between align-items-center" ] [
                    div [] [
                        input [ 
                            _type "checkbox"
                            _class "form-check-input me-2"
                            if todo.IsCompleted then _checked
                            attr "hx-post" (sprintf "/todos/toggle/%d" todo.Id)
                            attr "hx-target" "#todo-list-container"  // Target only the list
                            attr "hx-swap" "outerHTML"
                        ]
                        span [ 
                            _class (if todo.IsCompleted then "text-decoration-line-through" else "") 
                        ] [ str todo.Text ]
                    ]
                    button [
                        _class "btn btn-danger btn-sm"
                        attr "hx-delete" (sprintf "/todos/%d" todo.Id)
                        attr "hx-target" "#todo-list-container"  // Target only the list
                        attr "hx-swap" "outerHTML"
                    ] [ str "Delete" ]
                ]
        ]
    ]

let todoView (todos: TodoItem list) =
    div [] [
        h2 [] [ str "Todo List" ]
        todoAddForm()  // Keep form separate
        todoListView todos  // List in separate container
    ]
let weatherView (weather: WeatherData option) =
    div [ _class "card mt-4" ] [
        div [ _class "card-header" ] [ str "Weather" ]
        div [ _class "card-body" ] [
            match weather with
            | Some w ->
                p [] [ str (sprintf "Temperature: %.1f°C" w.Temperature) ]
                p [] [ str (sprintf "Condition: %s" w.Description) ]
            | None ->
                p [] [ str "Weather data unavailable" ]
        ]
    ]

let imageView (imageUrl: string option) =
    div [ _class "card mt-4" ] [
        div [ _class "card-header" ] [ str "Daily Image" ]
        div [ _class "card-body" ] [
            match imageUrl with
            | Some url ->
                img [ _src url; _class "img-fluid" ]
            | None ->
                p [] [ str "Image unavailable" ]
        ]
    ]

let indexView (state: AppState) =
    layout [
        div [] [
            todoView state.Todos
            weatherView state.Weather
            imageView state.ImageUrl
        ]
    ]

// Handlers
let handleGetTodos : HttpHandler =
    fun next ctx ->
        htmlView (indexView appState) next ctx

let handleAddTodo : HttpHandler =
    fun next ctx ->
        task {
            let! form = ctx.Request.ReadFormAsync()
            match form.TryGetValue("text") with
            | true, textValues when textValues.Count > 0 ->
                let newTodo = 
                    { Id = (match appState.Todos with [] -> 1 | xs -> (List.maxBy (fun x -> x.Id) xs).Id + 1)
                      Text = textValues.[0]
                      IsCompleted = false }
                
                appState <- { appState with Todos = newTodo :: appState.Todos }
                return! htmlView (todoListView appState.Todos) next ctx
            | _ ->
                return! RequestErrors.BAD_REQUEST (text "Missing todo text") next ctx
        }

let handleToggleTodo (id: int) : HttpHandler =
    fun next ctx ->
        task {
            let updatedTodos =
                appState.Todos
                |> List.map (fun todo ->
                    if todo.Id = id then
                        { todo with IsCompleted = not todo.IsCompleted }
                    else todo)
            
            appState <- { appState with Todos = updatedTodos }
            return! htmlView (todoListView appState.Todos) next ctx  // Return only list
        }

let handleDeleteTodo (id: int) : HttpHandler =
    fun next ctx ->
        task {
            let updatedTodos = appState.Todos |> List.filter (fun todo -> todo.Id <> id)
            appState <- { appState with Todos = updatedTodos }
            return! htmlView (todoListView appState.Todos) next ctx  // Return only list
        }

let sendDailyEmail () =
    task {
        let! weather = getWeatherData ()
        let! imageUrl = getRandomImage ()
        
        appState <- { appState with Weather = weather; ImageUrl = imageUrl }
        
        let todosText = 
            match appState.Todos with
            | [] -> "No todos today!"
            | todos ->
                todos
                |> List.map (fun t -> sprintf "- [%s] %s" (if t.IsCompleted then "X" else " ") t.Text)
                |> String.concat "\n"
        
        let weatherText =
            match weather with
            | Some w -> sprintf "Weather: %s, %.1f°C" w.Description w.Temperature
            | None -> "Weather data unavailable"
        
        let imageText =
            match imageUrl with
            | Some url -> sprintf "Daily image: %s" url
            | None -> "No image today"
        
        let body = sprintf "Good morning!\n\nYour todos:\n%s\n\n%s\n\n%s" todosText weatherText imageText
        
        let! result = sendEmail "Your Daily Todo & Weather Update" body
        match result with
        | Ok _ -> printfn "Daily email sent successfully"
        | Error e -> printfn "Failed to send email: %s" e
    }

// Scheduler for daily emails
let startEmailScheduler () =
    let timer = new System.Timers.Timer()
    timer.Interval <- 24.0 * 60.0 * 60.0 * 1000.0 // 24 hours
    timer.Elapsed.Add(fun _ -> 
        let now = DateTime.Now
        if now.Hour = 6 && now.Minute = 0 then // 6:00 AM
            sendDailyEmail() |> Async.AwaitTask |> Async.RunSynchronously)
    timer.Start()

    // Also send immediately on startup for testing
    sendDailyEmail() |> Async.AwaitTask |> Async.RunSynchronously

// Routes
let routes = [
    GET [
        route "/" handleGetTodos
    ]
    POST [
        route "/todos" handleAddTodo
        routef "/todos/toggle/%i" handleToggleTodo
    ]
    DELETE [
        routef "/todos/%i" handleDeleteTodo
    ]
]

// App configuration
let configureApp (app : IApplicationBuilder) =
    app.UseStaticFiles()
       .UseRouting()
       .UseGiraffe routes

let configureServices (services : IServiceCollection) =
    services.AddGiraffe() |> ignore

let configureLogging (builder : ILoggingBuilder) =
    builder.AddConsole()
           .AddDebug() |> ignore

[<EntryPoint>]
let main args =
    // Initialize app state
    task {
        let! weather = getWeatherData ()
        let! imageUrl = getRandomImage ()
        appState <- { Todos = []; Weather = weather; ImageUrl = imageUrl }
    } |> Async.AwaitTask |> Async.RunSynchronously
    
    // Start email scheduler
    startEmailScheduler()
    
    Host.CreateDefaultBuilder(args)
        .ConfigureWebHostDefaults(fun webHostBuilder ->
            webHostBuilder
                .Configure(fun context app ->
                    configureApp app |> ignore)
                .ConfigureServices(configureServices)
                .ConfigureLogging(configureLogging)
                |> ignore)
        .Build()
        .Run()
    0