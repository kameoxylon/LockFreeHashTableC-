#! /cygdrive/c/Perl/bin/perl

require v5.6.0;

use Env;

# Location of benchmark code (class files)
$codedir = "../bin/Release";

1;				# return true


# Benchmark program
$benchmark = "SXM.SkipList";

# Contention manager
@managers = (
	     "Aggressive",
	     "Backoff",
	     "Priority",
	     "Wait",
	     "MIS");

# Number of millisecond to run each benchmark
$millis = 60000;

###
### Run the benchmark
###

$prog = "SXM";

$| = 1;				# flush stdout immediately after printf

chdir($codedir) or die "Cannot cd to ".$codedir;

foreach $m (@managers) {
  $manager = "SXM.".$m."Manager";
  foreach $eflag (0, 1, 10, 50, 100) {
    foreach $thread (1, 2, 4, 8, 16, 32) {
      $command = "$prog -m $manager -b $benchmark -t $thread -n $millis -e $eflag";
      my $now = localtime;
      print "$now\t$command\n";
      system("$command") == 0 or die "command failed: $!\n";
    }
  }
}
