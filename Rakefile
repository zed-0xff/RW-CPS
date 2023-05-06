require 'fileutils'

task default: [:release, :clean]

desc "clean"
task :clean do
  my_name = File.basename(File.expand_path(".")) + ".dll"
  Dir["Assemblies/*"].each do |fname|
    File.unlink(fname) unless File.basename(fname) == my_name
  end
  Dir["Plugins/*"].each do |fname|
    File.unlink(fname) unless File.extname(fname) == ".dll"
  end
end

task :list do
  system "find . -type f -not -path './.git/*'", exception: true
  puts
  system "du -sh .", exception: true
end

desc "release"
task :release => [:mod, :plugins]

task :mod do
  Dir.chdir "Source"
  system "dotnet build -c Release", exception: true
  FileUtils.rm_rf "obj"
  Dir.chdir ".."
end

task :plugins do
  Dir.chdir "Source/Plugins"
  Dir["*.csproj"].each do |fname|
    system "dotnet build -c Release #{fname}", exception: true
  end
  FileUtils.rm_rf "obj"
  Dir.chdir "../.."
end
