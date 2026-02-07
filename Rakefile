require "rimtool/rake_tasks"

desc "release"
task :release => [:mod, :plugins]

# compile plugins before running tests
Rake::Task[:test].enhance [:plugins]

task :plugins

%w[1.4 1.5 1.6].each do |ver|
  desc "build plugins for #{ver}"
  task "plugins:build:#{ver}" => "build:#{ver}" do
    Dir.chdir "Source/Plugins" do
      Dir["*.csproj"].each do |fname|
        sh "dotnet build -c Release -p:RimWorldVersion=#{ver} #{fname}"
      end
    end
  end

  Rake::Task["plugins"].enhance ["plugins:build:#{ver}"]
end
