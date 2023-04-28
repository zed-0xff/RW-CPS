require 'fileutils'

task default: [:release, :clean]

desc "clean"
task :clean do
  FileUtils.rm_rf "Source/obj"
  my_name = File.basename(File.expand_path(".")) + ".dll"
  Dir["Assemblies/net472/*"].each do |fname|
    File.unlink(fname) unless File.basename(fname) == my_name
  end
  puts
  system "find . -type f -not -path './.git/*'", exception: true
  puts
  system "du -sh .", exception: true
end

desc "release"
task :release do
  Dir.chdir "Source"
  system "dotnet build -c Release", exception: true
  Dir.chdir ".."
end
