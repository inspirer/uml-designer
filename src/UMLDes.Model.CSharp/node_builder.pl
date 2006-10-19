

%classvars = ();
$curr = "";
$methods = "";
$state = 0;

open( NODES, "< Nodes.cs" ) or die;
while( <NODES> ) {
	$curr.= $_;
	if( /^\s*\/\/\s+\{\{(\w+)\}\}\s*$/ ) {
		my $name = $1;
		if( $name eq "Nodes" ) {
			BR2: while(<NODES>) {
				$curr.= $_;
				if( /^\s*\/\/\s+\{\{(\w+)\}\}\s*$/ ) {
					last BR2 if( $1 eq "End" );
					die "unknown: $1\n";
				}
				if( /^\s+\w+\s+class\s+(\w+)\s*:\s*(\w+)\s*\{(\s*\/\/\s*\{((\w+,?)+)\}\s*)?(\(\s*(.*)\s*\)\s*(\{\s*((.*;)+)\s*\}\s*)?)?$/ ) {
					$current_class = $1;
					$current_base = $2;
					$kinds = $4;
					$extra_params = $7;
					$dopcode = $9;
					$dopcode =~ s/;\s*/;\n\t\t\t/g;
					
					@vars = ();
					if ( exists $classvars{$current_base} ) {
						$ptr = $classvars{$current_base};
						for( @$ptr ) {
							push @vars, $_;
						}
					}
				} elsif( /^\s+internal\s+([\w\.]+)\s+(\w+)(\s*=.*)?;\s*(\/\/\s*!\s*)?$/ ) {
					push @vars, [$1,$2] unless $4 ne "";

				} elsif( /^\s*\}\s*$/ ) {
					# generate method
					$classvars{$current_class} = [@vars] if $#vars >= 0;
					if( $kinds ne "" ) {
						my $params = "";
						$params = "Kind k, " if( $kinds =~ /,/ );
						foreach( @vars ) {
							$params .= $_->[0]. " ". $_->[1].", ";
						}
						$params.= $extra_params.", " if( $extra_params ne "" );
						$params .= "Symbol s";
						$current_class_meth = $current_class;
						$current_class_meth =~ s/(Node|Decl)$//;
						$methods .= "\t\tinternal static $current_class $current_class_meth( $params ) {\n";
						$methods .= "\t\t\t$current_class res = new $current_class();\n";
						if( $kinds =~ /,/ ) {
							$methods .= "\t\t\tres.kind = k;\n";
						} else {
							$methods .= "\t\t\tres.kind = Kind.$kinds;\n";
						}
						$methods .= "\t\t\tres.start = s.pos;\n";
						$methods .= "\t\t\tres.end = s.endpos;\n";
						foreach( @vars ) {
							$methods .= "\t\t\tres.".$_->[1]." = ".$_->[1].";\n";
						}
						$methods .= "\t\t\t".$dopcode;
						$methods .= "return res;\n";
						$methods .= "\t\t}\n\n";
					}

				} elsif( /^\s*internal\s*enum/ ) {
					# nop					
				} else {
					die "$.: wrong line: $_" unless /^\s*$/;
				}
			}
		} elsif( $name eq "Methods" ) {
			$curr .= "\n".$methods;
			BR: while(<NODES>) {
				if( /^\s*\/\/\s+\{\{(\w+)\}\}\s*$/ ) {
					$curr.=$_;
					last BR if( $1 eq "End" );
					die "unknown: $1\n";
				}
			}

		} else {
			die "unknown: $name\n";
		}
	}
}
close( NODES );

open( N, "> Nodes.cs" );
print N $curr;
close( N );
