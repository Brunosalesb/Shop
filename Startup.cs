using System.Linq;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Shop.Data;

namespace Shop
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.

        //dizemos quais serviços iremos utilizar
        public void ConfigureServices(IServiceCollection services)
        {
            //evitar problemas com CrossOrigin
            services.AddCors();

            //comprimir JSON antes de enviar para a tela
            services.AddResponseCompression(opt =>
            {
                opt.Providers.Add<GzipCompressionProvider>();
                opt.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(new[] { "application/json" });
            });

            //add cache de maneira geral a aplicação
            // services.AddResponseCaching();

            services.AddControllers();

            //configurando autenticacao
            var key = Encoding.ASCII.GetBytes(Settings.Secret);
            services.AddAuthentication(x =>
            {
                x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            }).AddJwtBearer(x =>
            {
                x.RequireHttpsMetadata = false;
                x.SaveToken = true;
                x.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = false,
                    ValidateAudience = false
                };
            });

            //adicionando dataContext
            // services.AddDbContext<DataContext>(opt => opt.UseInMemoryDatabase("Database"));
            services.AddDbContext<DataContext>(opt => opt.UseSqlServer(Configuration.GetConnectionString("connectionString")));

            //AddScoped garante que só tem 1 dataContext por requisicao, e quando a requisicao acaba, trata de destruir o dataContext, assim destruindo a conexao com o banco de dados
            // services.AddScoped<DataContext, DataContext>();

            // addTransient toda vez que for pedido um dataContext, ele irá criar uma novo dataContext, abrindo uma nova conexao com o banco de dados
            // services.AddTransient<DataContext, DataContext>();

            // addSingleton cria um dataContext por aplicação, ou seja, quando a aplicacao iniciar, será criado um dataContext, que sera utilizado em todas requisicoes
            // não indicado quando se tem mais de um usuario utilizando a aplicacao
            // services.AddSingleton<DataContext, DataContext>();

            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "Shop API", Version = "v1" });
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.

        //dizemos como ou quais servicos adicionados no ConfigureServices iremos utilizar
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            //usado para saver se esta em tempo de desenvolvimento
            if (env.IsDevelopment())
            {
                //da mais informacoes do erro
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Shop V1"));
            }

            //forçar api responder sobre https
            app.UseHttpsRedirection();

            app.UseSwagger();
            app.UseSwaggerUI(x =>
            {
                x.SwaggerEndpoint("/swagger/v1/swagger.json", "Shop API V1");
            });

            //utilizar roteamento
            app.UseRouting();

            //chamadas localhost
            app.UseCors(x => x.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());

            //autenticacao
            app.UseAuthentication();
            app.UseAuthorization();

            //mapeamento dos endpoints
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
