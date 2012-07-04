
create table Test.tTest
(
	Id int not null,
	RefId int not null,
	constraint PK_tTest primary key (Id),
	constraint FK_tTest_RefId foreign key (Id) references Test.tTestFromDependentModel(Id)
);

